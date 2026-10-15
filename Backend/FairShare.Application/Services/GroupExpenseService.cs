using AutoMapper;
using FairShare.Application.Common.Exceptions;
using FairShare.Application.Common.Extensions;
using FairShare.Application.Common.Notifications;
using FairShare.Application.DTOs.GroupExpenses;
using FairShare.Application.Interfaces;
using FairShare.Domain.Entities;
using FairShare.Domain.Entities.Enums;
using FairShare.Domain.Interfaces;
using FairShare.Domain.Services;

namespace FairShare.Application.Services;

public class GroupExpenseService : IGroupExpenseService
{
    private const int MaxPageSize = 100;

    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly IMapper _mapper;

    public GroupExpenseService(IUnitOfWork unitOfWork, INotificationService notificationService, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _mapper = mapper;
    }

    public async Task<GroupExpenseResponse> CreateAsync(
        Guid groupId,
        CreateGroupExpenseRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        // Request shape (amount, participants, sums, duplicates) is checked by
        // CreateGroupExpenseRequestValidator. Here we check what only the database knows.
        var group = await LoadGroupAsync(groupId, cancellationToken);
        if (group.Members.All(m => m.UserId != currentUserId))
            throw new ForbiddenException("Само члан групе може додати групни трошак.", ErrorCodes.NotGroupMember);

        var category = await _unitOfWork.Categories.GetByIdAsync(request.CategoryId, cancellationToken)
            ?? throw new NotFoundException("Категорија није пронађена.", ErrorCodes.CategoryNotFound);

        var usersById = GetMemberUsers(group);
        var payer = ResolvePayer(request, usersById);
        var splits = CalculateSplits(request, usersById);

        var groupExpense = _mapper.Map<GroupExpense>(request);
        groupExpense.GroupId = groupId;
        groupExpense.Category = category;
        groupExpense.PaidByUser = payer;

        foreach (var split in splits)
            split.GroupExpenseId = groupExpense.Id;

        groupExpense.Splits = splits;

        await _unitOfWork.GroupExpenses.AddAsync(groupExpense, cancellationToken);

        // A new expense changes who owes whom, so earlier suggestions are no longer valid.
        await _unitOfWork.RemoveProposedSettlementsAsync(groupId, null, cancellationToken);

        // The expense, its splits and the removal of old suggestions are saved in one
        // SaveChanges call, i.e. one transaction.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await NotifyParticipantsAsync(group, groupExpense, category.Name, currentUserId, cancellationToken);

        return _mapper.Map<GroupExpenseResponse>(groupExpense);
    }

    public async Task<GroupExpenseResponse> GetByIdAsync(
        Guid groupId,
        Guid groupExpenseId,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        await EnsureMemberAsync(groupId, currentUserId, cancellationToken);

        var expense = await LoadExpenseAsync(groupId, groupExpenseId, cancellationToken);
        var response = _mapper.Map<GroupExpenseResponse>(expense);
        await FillCommentCountsAsync([response], cancellationToken);
        return response;
    }

    public async Task<IReadOnlyList<GroupExpenseResponse>> GetByGroupAsync(
        Guid groupId,
        int page,
        int pageSize,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        await EnsureMemberAsync(groupId, currentUserId, cancellationToken);

        // Query parameters are clamped instead of rejected - a too large page size is not an error.
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var expenses = await _unitOfWork.GroupExpenses.GetByGroupAsync(groupId, page, pageSize, cancellationToken);
        var responses = _mapper.Map<List<GroupExpenseResponse>>(expenses);
        await FillCommentCountsAsync(responses, cancellationToken);
        return responses;
    }

    public async Task<GroupExpenseResponse> UpdateAsync(
        Guid groupId,
        Guid groupExpenseId,
        UpdateGroupExpenseRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var group = await LoadGroupAsync(groupId, cancellationToken);
        var expense = await LoadExpenseAsync(groupId, groupExpenseId, cancellationToken);

        EnsureCanModify(group, expense, currentUserId);

        var category = await _unitOfWork.Categories.GetByIdAsync(request.CategoryId, cancellationToken)
            ?? throw new NotFoundException("Категорија није пронађена.", ErrorCodes.CategoryNotFound);

        var usersById = GetMemberUsers(group);
        var payer = ResolvePayer(request, usersById);
        var newSplits = CalculateSplits(request, usersById);

        // Splits are replaced as a whole: the old ones are deleted and new ones inserted.
        // If two members edit the same expense at the same moment, the second request finds
        // the old splits already gone and fails with a concurrency error (HTTP 409) instead of
        // leaving the expense with two sets of splits.
        foreach (var oldSplit in expense.Splits.ToList())
            _unitOfWork.ExpenseSplits.Remove(oldSplit);

        // Overwrites amount, date, description, category, payer and split type.
        _mapper.Map(request, expense);
        expense.Category = category;
        expense.PaidByUser = payer;

        foreach (var split in newSplits)
        {
            split.GroupExpenseId = expense.Id;
            await _unitOfWork.ExpenseSplits.AddAsync(split, cancellationToken);
        }

        // The balances changed, so earlier suggestions are no longer valid.
        await _unitOfWork.RemoveProposedSettlementsAsync(groupId, null, cancellationToken);

        // Everything above is one transaction: the edit is applied completely or not at all.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var response = _mapper.Map<GroupExpenseResponse>(expense);
        response.Splits = _mapper.Map<List<ExpenseSplitResponse>>(newSplits);
        return response;
    }

    public async Task DeleteAsync(
        Guid groupId,
        Guid groupExpenseId,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var group = await LoadGroupAsync(groupId, cancellationToken);
        var expense = await LoadExpenseAsync(groupId, groupExpenseId, cancellationToken);

        EnsureCanModify(group, expense, currentUserId);

        // Splits and comments are removed together with the expense (cascade delete).
        _unitOfWork.GroupExpenses.Remove(expense);
        await _unitOfWork.RemoveProposedSettlementsAsync(groupId, null, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    // ---------- helpers ----------

    /// <summary>
    /// Every participant except the member who entered the expense learns about it and about
    /// their share - in the app only, because a busy group would otherwise flood the inbox.
    /// </summary>
    private async Task NotifyParticipantsAsync(
        Group group,
        GroupExpense expense,
        string categoryName,
        Guid currentUserId,
        CancellationToken cancellationToken)
    {
        var title = NotificationText.Quote(string.IsNullOrWhiteSpace(expense.Description) ? categoryName : expense.Description);
        var total = NotificationText.Money(expense.Amount, group.Currency);

        foreach (var split in expense.Splits.Where(s => s.UserId != currentUserId))
        {
            await _notificationService.NotifyAsync(
                split.UserId,
                NotificationType.GroupExpenseAdded,
                "Нови групни трошак",
                $"Нови трошак у групи {NotificationText.Quote(group.Name)}: {title}, укупно {total}. " +
                $"Твој дио је {NotificationText.Money(split.Amount, group.Currency)}.",
                cancellationToken,
                sendEmail: false);
        }
    }

    /// <summary>Loads the group together with its members and their users (tracked).</summary>
    /// <summary>Number of comments of every listed expense - one grouped query for the whole page.</summary>
    private async Task FillCommentCountsAsync(IReadOnlyList<GroupExpenseResponse> responses, CancellationToken cancellationToken)
    {
        if (responses.Count == 0)
            return;

        var counts = await _unitOfWork.Comments.CountByGroupExpensesAsync(
            responses.Select(r => r.Id).ToList(), cancellationToken);

        foreach (var response in responses)
            response.CommentCount = counts.GetValueOrDefault(response.Id, 0);
    }

    private async Task<Group> LoadGroupAsync(Guid groupId, CancellationToken cancellationToken)
        => await _unitOfWork.Groups.GetWithMembersAsync(groupId, cancellationToken)
           ?? throw new NotFoundException("Група није пронађена.", ErrorCodes.GroupNotFound);

    /// <summary>Loads the expense with all details and checks that it belongs to the given group.</summary>
    private async Task<GroupExpense> LoadExpenseAsync(
        Guid groupId,
        Guid groupExpenseId,
        CancellationToken cancellationToken)
    {
        var expense = await _unitOfWork.GroupExpenses.GetWithDetailsAsync(groupExpenseId, cancellationToken);

        // An expense of another group is reported exactly like a missing one, so the response
        // does not reveal that it exists.
        if (expense is null || expense.GroupId != groupId)
            throw new NotFoundException("Групни трошак није пронађен.", ErrorCodes.GroupExpenseNotFound);

        return expense;
    }

    private async Task EnsureMemberAsync(Guid groupId, Guid userId, CancellationToken cancellationToken)
    {
        if (!await _unitOfWork.GroupMembers.IsMemberAsync(groupId, userId, cancellationToken))
            throw new ForbiddenException("Само члан групе може видјети трошкове групе.", ErrorCodes.NotGroupMember);
    }

    /// <summary>
    /// Who may edit or delete an expense: the member who paid it, or the group owner.
    /// An expense that involves a former member is frozen - changing it would give a balance
    /// to someone who is no longer in the group and can no longer settle it.
    /// </summary>
    private static void EnsureCanModify(Group group, GroupExpense expense, Guid currentUserId)
    {
        var membership = group.Members.FirstOrDefault(m => m.UserId == currentUserId)
            ?? throw new ForbiddenException("Нисте члан ове групе.", ErrorCodes.NotGroupMember);

        var isPayer = expense.PaidByUserId == currentUserId;
        var isOwner = membership.Role == GroupRole.Owner;
        if (!isPayer && !isOwner)
            throw new ForbiddenException("Трошак може мијењати или брисати само члан који га је платио или власник групе.", ErrorCodes.GroupExpenseModifyForbidden);

        var memberIds = group.Members.Select(m => m.UserId).ToHashSet();
        var involvesFormerMember =
            !memberIds.Contains(expense.PaidByUserId) ||
            expense.Splits.Any(s => !memberIds.Contains(s.UserId));

        if (involvesFormerMember)
            throw new ConflictException("Трошак у којем учествује бивши члан групе није могуће мијењати нити брисати.", ErrorCodes.GroupExpenseFormerMember);
    }

    private static Dictionary<Guid, User> GetMemberUsers(Group group)
        => group.Members.ToDictionary(m => m.UserId, m => m.User);

    private static User ResolvePayer(CreateGroupExpenseRequest request, Dictionary<Guid, User> usersById)
        => usersById.TryGetValue(request.PaidByUserId, out var payer)
            ? payer
            : throw new NotFoundException("Корисник који је платио трошак није члан ове групе.", ErrorCodes.PayerNotMember);

    /// <summary>
    /// Checks that every participant is a member of the group and calculates the splits.
    /// The calculation itself (including rounding remainders) is pure domain logic.
    /// </summary>
    private static List<ExpenseSplit> CalculateSplits(
        CreateGroupExpenseRequest request,
        Dictionary<Guid, User> usersById)
    {
        foreach (var participant in request.Participants)
        {
            if (!usersById.ContainsKey(participant.UserId))
                throw new NotFoundException("Неко од учесника трошка није члан ове групе.", ErrorCodes.ParticipantNotMember);
        }

        var splitInputs = request.Participants
            .Select(p => new SplitParticipantInput(p.UserId, p.Amount, p.Percentage))
            .ToList();

        var splits = ExpenseSplitCalculator.Calculate(request.Amount, request.SplitType, splitInputs);

        // Attach the tracked user to every split, so the response can show names.
        foreach (var split in splits)
            split.User = usersById[split.UserId];

        return splits;
    }
}
