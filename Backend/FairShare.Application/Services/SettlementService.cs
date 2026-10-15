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
            groupId, currentUserId, "Само члан групе може видјети стање групе.", cancellationToken);

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
            groupId, currentUserId, "Само члан групе може израчунати приједлог поравнања.", cancellationToken);

        var usersById = group.Members.ToDictionary(m => m.UserId, m => m.User);
        var balances = await _unitOfWork.GetGroupNetBalancesAsync(groupId, cancellationToken);

        // Members can leave only with a settled balance, so this should never happen. The check
        // protects the algorithm: it relies on the balances of the listed users adding up to zero.
        if (balances.Any(b => !usersById.ContainsKey(b.Key) && !GroupBalanceCalculator.IsSettled(b.Value)))
            throw new ConflictException("У групи постоји неизмирен салдо корисника који више није члан, па приједлог није могуће израчунати.", ErrorCodes.FormerMemberBalance);

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
            throw new NotFoundException("Поравнање није пронађено.", ErrorCodes.SettlementNotFound);

        if (transaction.DebtorUserId != currentUserId && transaction.CreditorUserId != currentUserId)
            throw new ForbiddenException("Поравнање може означити као измирено само члан који дугује или члан коме се дугује.", ErrorCodes.SettlementNotParticipant);

        if (transaction.Status == SettlementStatus.Settled)
            throw new ConflictException("Поравнање је већ измирено.", ErrorCodes.SettlementAlreadySettled);

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
            throw new NotFoundException("Поравнање није пронађено.", ErrorCodes.SettlementNotFound);

        if (transaction.CreditorUserId != currentUserId)
            throw new ForbiddenException("Подсјетник може послати само члан коме се дугује.", ErrorCodes.ReminderOnlyCreditor);

        if (transaction.Status == SettlementStatus.Settled)
            throw new ConflictException("Поравнање је већ измирено.", ErrorCodes.SettlementAlreadySettled);

        var now = DateTime.UtcNow;
        if (transaction.NextReminderAllowedAt(now) is not null)
            throw new ConflictException(
                "Подсјетник за ово дуговање је већ послат. Нови можеш послати 24 сата након претходног.",
                ErrorCodes.ReminderTooSoon);

        transaction.MarkReminderSent(now);

        // Saved before the notification is sent. Two clicks at the same moment: the row version
        // (xmin) lets only one UPDATE through, the other gets HTTP 409 - one reminder, not two.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var group = await _unitOfWork.Groups.GetByIdAsync(groupId, cancellationToken);
        var creditor = NotificationText.FullName(transaction.CreditorUser);
        var text = $"{creditor} те подсјећа на дуг у групи {NotificationText.Quote(group?.Name ?? string.Empty)}: " +
                   $"треба да уплатиш {NotificationText.Money(transaction.Amount, group?.Currency)}.";

        // The personal message is plain text: the app shows it as text and the e-mail template
        // HTML-encodes it, so it cannot inject anything.
        var personal = message?.Trim();
        if (!string.IsNullOrEmpty(personal))
            text += $" Порука: {NotificationText.Quote(personal)}";

        await _notificationService.NotifyAsync(
            transaction.DebtorUserId,
            NotificationType.DebtReminder,
            "Подсјетник за дуг",
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
            throw new ForbiddenException("Само члан групе може видјети поравнања групе.", ErrorCodes.NotGroupMember);

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
            ?? throw new NotFoundException("Група није пронађена.", ErrorCodes.GroupNotFound);

        if (group.Members.All(m => m.UserId != currentUserId))
            throw new ForbiddenException(forbiddenMessage, ErrorCodes.NotGroupMember);

        return group;
    }
}