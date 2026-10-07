using FairShare.Application.DTOs.Settlements;
using FairShare.Application.Interfaces;
using FairShare.Domain.Entities;
using FairShare.Domain.Entities.Enums;
using FairShare.Domain.Interfaces;
using FairShare.Domain.Services;
using TimeSheet.Application.Common.Exceptions;

namespace FairShare.Application.Services;

public class SettlementService : ISettlementService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService; // НОВО

    public SettlementService(IUnitOfWork unitOfWork, INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
    }

    public async Task<IReadOnlyList<BalanceResponse>> GetGroupBalancesAsync(
        Guid groupId,
        CancellationToken cancellationToken = default)
    {
        var group = await _unitOfWork.Groups.GetWithMembersAsync(groupId, cancellationToken)
            ?? throw new NotFoundException("Група није пронађена.");

        var balances = await CalculateNetBalancesAsync(groupId, cancellationToken);

        return group.Members
            .Select(m => new BalanceResponse
            {
                UserId = m.UserId,
                FirstName = m.User.FirstName,
                LastName = m.User.LastName,
                NetBalance = balances.GetValueOrDefault(m.UserId, 0m)
            })
            .ToList();
    }

    public async Task<IReadOnlyList<SettlementTransactionResponse>> GenerateSettlementSuggestionsAsync(
        Guid groupId,
        bool useExactAlgorithm,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        if (!await _unitOfWork.GroupMembers.IsMemberAsync(groupId, currentUserId, cancellationToken))
            throw new ForbiddenException("Само члан групе може генерисати приједлог поравнања.");

        var group = await _unitOfWork.Groups.GetWithMembersAsync(groupId, cancellationToken)
            ?? throw new NotFoundException("Група није пронађена.");

        var balances = await CalculateNetBalancesAsync(groupId, cancellationToken);

        // Стари, још неизмирени приједлози се замјењују новим прерачуном
        var oldProposed = await _unitOfWork.SettlementTransactions.GetByGroupAsync(
            groupId, SettlementStatus.Proposed, cancellationToken);
        foreach (var old in oldProposed)
            _unitOfWork.SettlementTransactions.Remove(old);

        var suggestions = useExactAlgorithm
            ? DebtSimplifier.SimplifyExact(balances)
            : DebtSimplifier.SimplifyGreedy(balances);

        var usersById = group.Members.ToDictionary(m => m.UserId, m => m.User);

        var transactions = new List<SettlementTransaction>();
        foreach (var suggestion in suggestions)
        {
            var transaction = new SettlementTransaction
            {
                GroupId = groupId,
                Group = group,
                DebtorUserId = suggestion.DebtorUserId,
                DebtorUser = usersById[suggestion.DebtorUserId],
                CreditorUserId = suggestion.CreditorUserId,
                CreditorUser = usersById[suggestion.CreditorUserId],
                Amount = suggestion.Amount,
                Status = SettlementStatus.Proposed,
                CreatedAt = DateTime.UtcNow
            };
            transaction.GenerateQrCode();

            transactions.Add(transaction);
            await _unitOfWork.SettlementTransactions.AddAsync(transaction, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // НОВО: сваки дужник добија обавјештење о свом дијелу поравнања
        foreach (var t in transactions)
        {
            await _notificationService.NotifyAsync(
                t.DebtorUserId,
                NotificationType.SettlementSuggested,
                "Нови приједлог поравнања",
                $"У групи '{group.Name}' предложено је поравнање: дугујете кориснику " +
                $"{t.CreditorUser.FirstName} {t.CreditorUser.LastName} износ од {t.Amount} {group.Currency}.",
                cancellationToken);
        }

        return transactions.Select(MapToResponse).ToList();
    }

    public async Task<SettlementTransactionResponse> MarkAsSettledAsync(
        Guid settlementTransactionId,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var transaction = await _unitOfWork.SettlementTransactions.GetByIdWithDetailsAsync(
            settlementTransactionId, cancellationToken)
            ?? throw new NotFoundException("Трансакција поравнања није пронађена.");

        if (transaction.DebtorUserId != currentUserId && transaction.CreditorUserId != currentUserId)
            throw new ForbiddenException("Само дужник или повјерилац могу означити поравнање као измирено.");

        if (transaction.Status == SettlementStatus.Settled)
            throw new ConflictException("Трансакција је већ измирена.");

        transaction.Status = SettlementStatus.Settled;
        _unitOfWork.SettlementTransactions.Update(transaction);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // НОВО: обавјештава се друга страна (она која није кликнула "измирено")
        var group = await _unitOfWork.Groups.GetByIdAsync(transaction.GroupId, cancellationToken);
        var otherUserId = transaction.DebtorUserId == currentUserId
            ? transaction.CreditorUserId
            : transaction.DebtorUserId;

        await _notificationService.NotifyAsync(
            otherUserId,
            NotificationType.SettlementCompleted,
            "Поравнање измирено",
            $"Поравнање од {transaction.Amount} {group?.Currency} између корисника " +
            $"{transaction.DebtorUser.FirstName} {transaction.DebtorUser.LastName} и " +
            $"{transaction.CreditorUser.FirstName} {transaction.CreditorUser.LastName} " +
            $"у групи '{group?.Name}' означено је као измирено.",
            cancellationToken);

        return MapToResponse(transaction);
    }

    public async Task<IReadOnlyList<SettlementTransactionResponse>> GetGroupSettlementsAsync(
        Guid groupId,
        SettlementStatus? status,
        CancellationToken cancellationToken = default)
    {
        var transactions = await _unitOfWork.SettlementTransactions.GetByGroupAsync(groupId, status, cancellationToken);
        return transactions.Select(MapToResponse).ToList();
    }

    // ---------- рачунање нето салда ----------

    /// <summary>
    /// Нето салдо = (све што је корисник платио у групним трошковима) минус
    /// (све што тренутно дугује по подјелама), умањено за већ измирена поравнања.
    /// Позитивно = група му дугује, негативно = он дугује групи.
    /// </summary>
    private async Task<Dictionary<Guid, decimal>> CalculateNetBalancesAsync(
        Guid groupId,
        CancellationToken cancellationToken)
    {
        var balances = new Dictionary<Guid, decimal>();

        var groupExpenses = await _unitOfWork.GroupExpenses.GetAllByGroupAsync(groupId, cancellationToken);
        foreach (var expense in groupExpenses)
        {
            Add(balances, expense.PaidByUserId, expense.Amount);
            foreach (var split in expense.Splits)
                Add(balances, split.UserId, -split.Amount);
        }

        // Већ измирене трансакције смањују преостали дуг/потраживање
        var settled = await _unitOfWork.SettlementTransactions.GetByGroupAsync(
            groupId, SettlementStatus.Settled, cancellationToken);
        foreach (var s in settled)
        {
            Add(balances, s.DebtorUserId, s.Amount);
            Add(balances, s.CreditorUserId, -s.Amount);
        }

        return balances;
    }

    private static void Add(Dictionary<Guid, decimal> balances, Guid userId, decimal amount)
        => balances[userId] = balances.GetValueOrDefault(userId, 0m) + amount;

    private static SettlementTransactionResponse MapToResponse(SettlementTransaction t) => new()
    {
        Id = t.Id,
        GroupId = t.GroupId,
        DebtorUserId = t.DebtorUserId,
        DebtorName = $"{t.DebtorUser.FirstName} {t.DebtorUser.LastName}",
        CreditorUserId = t.CreditorUserId,
        CreditorName = $"{t.CreditorUser.FirstName} {t.CreditorUser.LastName}",
        Amount = t.Amount,
        Status = t.Status,
        CreatedAt = t.CreatedAt,
        QrPaymentData = t.QrPaymentData is null ? null : new QrPaymentDataResponse
        {
            RecipientAccount = t.QrPaymentData.RecipientAccount,
            Amount = t.QrPaymentData.Amount,
            Currency = t.QrPaymentData.Currency,
            ReferenceCode = t.QrPaymentData.ReferenceCode
        }
    };
}
