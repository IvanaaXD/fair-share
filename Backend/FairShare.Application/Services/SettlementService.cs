using AutoMapper;
using FairShare.Application.Common.Exceptions;
using FairShare.Application.Common.Extensions;
using FairShare.Application.Common.Notifications;
using FairShare.Application.DTOs.Settlements;
using FairShare.Application.Interfaces;
using FairShare.Domain.Entities;
using FairShare.Domain.Entities.Enums;
using FairShare.Domain.Interfaces;
using FairShare.Domain.Services;

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
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var group = await LoadGroupForMemberAsync(
            groupId, currentUserId, "Only a group member can view group balances.", cancellationToken);

        var balances = await _unitOfWork.GetGroupNetBalancesAsync(groupId, cancellationToken);

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
        var group = await LoadGroupForMemberAsync(
            groupId, currentUserId, "Only a group member can generate a settlement proposal.", cancellationToken);

        var usersById = group.Members.ToDictionary(m => m.UserId, m => m.User);
        var balances = await _unitOfWork.GetGroupNetBalancesAsync(groupId, cancellationToken);

        // Members can leave only with a settled balance, so this should never happen. The check
        // protects the algorithm: it relies on the balances of the listed users adding up to zero.
        if (balances.Any(b => !usersById.ContainsKey(b.Key) && !GroupBalanceCalculator.IsSettled(b.Value)))
            throw new ConflictException("There is an unsettled balance in the group for a user who is no longer a member, so the proposal cannot be generated.");

        var memberBalances = balances
            .Where(b => usersById.ContainsKey(b.Key))
            .ToDictionary(b => b.Key, b => b.Value);

        // Old, still unsettled suggestions are replaced by the new calculation.
        await _unitOfWork.RemoveProposedSettlementsAsync(groupId, null, cancellationToken);

        var suggestions = useExactAlgorithm
            ? DebtSimplifier.SimplifyExact(memberBalances)
            : DebtSimplifier.SimplifyGreedy(memberBalances);

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
        // If another request settled one of the old suggestions in the meantime, its row version
        // no longer matches and the whole operation fails with HTTP 409 instead of deleting a
        // settlement that was just paid.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Both sides of every transfer are told: the debtor what to pay (also by e-mail),
        // the creditor what to expect (in the app only).
        foreach (var t in transactions)
        {
            var amount = NotificationText.Money(t.Amount, group.Currency);
            var groupName = NotificationText.Quote(group.Name);

            await _notificationService.NotifyAsync(
                t.DebtorUserId,
                NotificationType.SettlementSuggested,
                "Нови приједлог поравнања",
                $"Нови приједлог поравнања у групи {groupName}: треба да уплатиш {amount}, прима {NotificationText.FullName(t.CreditorUser)}.",
                cancellationToken);

            await _notificationService.NotifyAsync(
                t.CreditorUserId,
                NotificationType.SettlementSuggested,
                "Нови приједлог поравнања",
                $"Нови приједлог поравнања у групи {groupName}: {NotificationText.FullName(t.DebtorUser)} треба да ти уплати {amount}.",
                cancellationToken,
                sendEmail: false);
        }

        return _mapper.Map<List<SettlementTransactionResponse>>(transactions);
    }

    public async Task<SettlementTransactionResponse> MarkAsSettledAsync(
        Guid groupId,
        Guid settlementTransactionId,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var transaction = await _unitOfWork.SettlementTransactions.GetByIdWithDetailsAsync(
            settlementTransactionId, cancellationToken);

        // A settlement of another group is reported exactly like a missing one.
        if (transaction is null || transaction.GroupId != groupId)
            throw new NotFoundException("Settlement transaction not found.");

        if (transaction.DebtorUserId != currentUserId && transaction.CreditorUserId != currentUserId)
            throw new ForbiddenException("Only the debtor or creditor can mark the settlement as settled.");

        if (transaction.Status == SettlementStatus.Settled)
            throw new ConflictException("The transaction is already settled.");

        transaction.Status = SettlementStatus.Settled;

        // Protection against double settlement: if two requests pass the status check above at
        // the same time, only the first UPDATE matches the row version. The second one throws
        // DbUpdateConcurrencyException, which the middleware turns into HTTP 409.
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
            $"Поравнање у групи {NotificationText.Quote(group?.Name ?? string.Empty)} је означено као измирено: " +
            $"{NotificationText.FullName(transaction.DebtorUser)} → {NotificationText.FullName(transaction.CreditorUser)}, " +
            $"{NotificationText.Money(transaction.Amount, group?.Currency)}.",
            cancellationToken);

        return _mapper.Map<SettlementTransactionResponse>(transaction);
    }

    public async Task<IReadOnlyList<SettlementTransactionResponse>> GetGroupSettlementsAsync(
        Guid groupId,
        SettlementStatus? status,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        if (!await _unitOfWork.GroupMembers.IsMemberAsync(groupId, currentUserId, cancellationToken))
            throw new ForbiddenException("Only a group member can view group settlements.");

        var transactions = await _unitOfWork.SettlementTransactions.GetByGroupAsync(groupId, status, cancellationToken);
        return _mapper.Map<List<SettlementTransactionResponse>>(transactions);
    }

    // ---------- helpers ----------

    /// <summary>Loads the group with its members and checks that the current user is one of them.</summary>
    private async Task<Group> LoadGroupForMemberAsync(
        Guid groupId,
        Guid currentUserId,
        string forbiddenMessage,
        CancellationToken cancellationToken)
    {
        var group = await _unitOfWork.Groups.GetWithMembersAsync(groupId, cancellationToken)
            ?? throw new NotFoundException("Group not found.");

        if (group.Members.All(m => m.UserId != currentUserId))
            throw new ForbiddenException(forbiddenMessage);

        return group;
    }
}