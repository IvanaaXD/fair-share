using AutoMapper;
using FairShare.Application.Abstractions;
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
    private readonly ICacheService _cache;

    public SettlementService(
        IUnitOfWork unitOfWork,
        INotificationService notificationService,
        IMapper mapper,
        ICacheService cache)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _mapper = mapper;
        _cache = cache;
    }

    public async Task<IReadOnlyList<BalanceResponse>> GetGroupBalancesAsync(
        Guid groupId,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var group = await LoadGroupForMemberAsync(
            groupId, currentUserId, "Only a group member can view group balances.", cancellationToken);

        // Membership was checked against the database above; only the numbers come from the cache.
        var balances = await _unitOfWork.GetCachedGroupNetBalancesAsync(_cache, groupId, cancellationToken);

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
            groupId, currentUserId, "Only a group member can calculate settlement suggestions.", cancellationToken);

        var usersById = group.Members.ToDictionary(m => m.UserId, m => m.User);

        // Fresh from the database, not from the cache: these amounts become transfers people pay.
        var balances = await _unitOfWork.GetGroupNetBalancesAsync(groupId, cancellationToken);

        // Members can leave only with a settled balance, so this should never happen. The check
        // protects the algorithm: it relies on the balances of the listed users adding up to zero.
        if (balances.Any(b => !usersById.ContainsKey(b.Key) && !GroupBalanceCalculator.IsSettled(b.Value)))
            throw new ConflictException("The group contains an unsettled balance of a user who is no longer a member, so the suggestion cannot be calculated.", ErrorCodes.FormerMemberBalance);

        var memberBalances = balances
            .Where(b => usersById.ContainsKey(b.Key))
            .ToDictionary(b => b.Key, b => b.Value);

        // The exact search is exponential, so it is offered only for small groups. Checked before
        // anything is changed, so a refused request leaves the old suggestions untouched.
        if (useExactAlgorithm &&
            memberBalances.Count(b => !GroupBalanceCalculator.IsSettled(b.Value)) > DebtSimplifier.MaxExactParticipants)
            throw new ConflictException(
                $"The exact algorithm is available for groups with at most {DebtSimplifier.MaxExactParticipants} members " +
                "with an unsettled balance. Use the standard algorithm.",
                ErrorCodes.ExactTooManyParticipants);

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
                "New settlement suggestion",
                $"New settlement suggestion in group {groupName}: you need to pay {amount}, receiver is {NotificationText.FullName(t.CreditorUser)}.",
                cancellationToken);

            await _notificationService.NotifyAsync(
                t.CreditorUserId,
                NotificationType.SettlementSuggested,
                "New settlement suggestion",
                $"New settlement suggestion in group {groupName}: {NotificationText.FullName(t.DebtorUser)} should pay you {amount}.",
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
            throw new NotFoundException("Settlement not found.", ErrorCodes.SettlementNotFound);

        if (transaction.DebtorUserId != currentUserId && transaction.CreditorUserId != currentUserId)
            throw new ForbiddenException("A settlement can only be marked as settled by the owing member or the receiving member.", ErrorCodes.SettlementNotParticipant);

        if (transaction.Status == SettlementStatus.Settled)
            throw new ConflictException("Settlement is already settled.", ErrorCodes.SettlementAlreadySettled);

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
            "Settlement settled",
            $"Settlement in group {NotificationText.Quote(group?.Name ?? string.Empty)} was marked as settled: " +
            $"{NotificationText.FullName(transaction.DebtorUser)} → {NotificationText.FullName(transaction.CreditorUser)}, " +
            $"{NotificationText.Money(transaction.Amount, group?.Currency)}.",
            cancellationToken);

        return _mapper.Map<SettlementTransactionResponse>(transaction);
    }

    public async Task<SettlementTransactionResponse> SendReminderAsync(
        Guid groupId,
        Guid settlementTransactionId,
        string? message,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var transaction = await _unitOfWork.SettlementTransactions.GetByIdWithDetailsAsync(
            settlementTransactionId, cancellationToken);

        // A settlement of another group is reported exactly like a missing one.
        if (transaction is null || transaction.GroupId != groupId)
            throw new NotFoundException("Settlement not found.", ErrorCodes.SettlementNotFound);

        if (transaction.CreditorUserId != currentUserId)
            throw new ForbiddenException("A reminder can only be sent by the member who is owed.", ErrorCodes.ReminderOnlyCreditor);

        if (transaction.Status == SettlementStatus.Settled)
            throw new ConflictException("Settlement is already settled.", ErrorCodes.SettlementAlreadySettled);

        var now = DateTime.UtcNow;
        if (transaction.NextReminderAllowedAt(now) is not null)
            throw new ConflictException(
                "A reminder for this debt has already been sent. You can send a new one 24 hours after the previous one.",
                ErrorCodes.ReminderTooSoon);

        transaction.MarkReminderSent(now);

        // Saved before the notification is sent. Two clicks at the same moment: the row version
        // (xmin) lets only one UPDATE through, the other gets HTTP 409 - one reminder, not two.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var group = await _unitOfWork.Groups.GetByIdAsync(groupId, cancellationToken);
        var creditor = NotificationText.FullName(transaction.CreditorUser);
        var text = $"{creditor} is reminding you of a debt in group {NotificationText.Quote(group?.Name ?? string.Empty)}: " +
                   $"you need to pay {NotificationText.Money(transaction.Amount, group?.Currency)}.";

        // The personal message is plain text: the app shows it as text and the e-mail template
        // HTML-encodes it, so it cannot inject anything.
        var personal = message?.Trim();
        if (!string.IsNullOrEmpty(personal))
            text += $" Message: {NotificationText.Quote(personal)}";

        await _notificationService.NotifyAsync(
            transaction.DebtorUserId,
            NotificationType.DebtReminder,
            "Debt reminder",
            text,
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
            throw new ForbiddenException("Only a group member can view group settlements.", ErrorCodes.NotGroupMember);

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
            ?? throw new NotFoundException("Group not found.", ErrorCodes.GroupNotFound);

        if (group.Members.All(m => m.UserId != currentUserId))
            throw new ForbiddenException(forbiddenMessage, ErrorCodes.NotGroupMember);

        return group;
    }
}