using AutoMapper;
using FairShare.Application.Common.Exceptions;
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
    private readonly INotificationService _notificationService;
    private readonly IMapper _mapper;

    public SettlementService(IUnitOfWork unitOfWork, INotificationService notificationService, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<BalanceResponse>> GetGroupBalancesAsync(
        Guid groupId,
        CancellationToken cancellationToken = default)
    {
        var group = await _unitOfWork.Groups.GetWithMembersAsync(groupId, cancellationToken)
            ?? throw new NotFoundException("Група није пронађена.");

        var balances = await CalculateNetBalancesAsync(groupId, cancellationToken);

        // Balances are calculated values, not entity fields, so they are built by hand.
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

        // Old, still unsettled suggestions are replaced by the new calculation.
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

        // Removing old suggestions and adding new ones happens in a single transaction.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Every debtor is notified about their part of the settlement.
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

        return _mapper.Map<List<SettlementTransactionResponse>>(transactions);
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

        // The other party (the one who did not click "settled") is notified.
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

        return _mapper.Map<SettlementTransactionResponse>(transaction);
    }

    public async Task<IReadOnlyList<SettlementTransactionResponse>> GetGroupSettlementsAsync(
        Guid groupId,
        SettlementStatus? status,
        CancellationToken cancellationToken = default)
    {
        var transactions = await _unitOfWork.SettlementTransactions.GetByGroupAsync(groupId, status, cancellationToken);
        return _mapper.Map<List<SettlementTransactionResponse>>(transactions);
    }

    // ---------- net balance calculation ----------

    /// <summary>
    /// Net balance = (everything the user paid for group expenses) minus (everything the user
    /// owes through splits), adjusted by settlements that were already completed.
    /// Positive = the group owes the user, negative = the user owes the group.
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

        // Completed settlements reduce the remaining debt / claim.
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
}
