using AutoMapper;
using FairShare.Application.Common.Exceptions;
using FairShare.Application.Common.Notifications;
using FairShare.Application.DTOs.Comments;
using FairShare.Application.Interfaces;
using FairShare.Domain.Entities;
using FairShare.Domain.Entities.Enums;
using FairShare.Domain.Interfaces;

namespace FairShare.Application.Services;

public class CommentService : ICommentService
{
    /// <summary>How much of the comment is quoted in the notification.</summary>
    private const int NotificationQuoteLength = 120;

    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly IMapper _mapper;

    public CommentService(IUnitOfWork unitOfWork, INotificationService notificationService, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _mapper = mapper;
    }

    public async Task<CommentResponse> CreateAsync(
        Guid groupId,
        Guid groupExpenseId,
        CreateCommentRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        // Text length is checked by CreateCommentRequestValidator.
        var (group, expense) = await LoadForMemberAsync(groupId, groupExpenseId, currentUserId, cancellationToken);

        // Loaded through the repository (tracked), so EF does not try to insert the user again.
        var author = await _unitOfWork.Users.GetByIdAsync(currentUserId, cancellationToken)
            ?? throw new NotFoundException("User not found.", ErrorCodes.UserNotFound);

        var comment = _mapper.Map<Comment>(request);
        comment.CreatedAt = DateTime.UtcNow;
        comment.UserId = currentUserId;
        comment.User = author;
        comment.GroupExpenseId = expense.Id;

        await _unitOfWork.Comments.AddAsync(comment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await NotifyAboutCommentAsync(group, expense, comment, cancellationToken);

        return _mapper.Map<CommentResponse>(comment);
    }

    public async Task<IReadOnlyList<CommentResponse>> GetByGroupExpenseAsync(
        Guid groupId,
        Guid groupExpenseId,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        await LoadForMemberAsync(groupId, groupExpenseId, currentUserId, cancellationToken);

        var comments = await _unitOfWork.Comments.GetByGroupExpenseAsync(groupExpenseId, cancellationToken);
        return _mapper.Map<List<CommentResponse>>(comments);
    }

    public async Task DeleteAsync(
        Guid groupId,
        Guid groupExpenseId,
        Guid commentId,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var (group, _) = await LoadForMemberAsync(groupId, groupExpenseId, currentUserId, cancellationToken);

        // A comment of another expense is reported exactly like a missing one.
        var comment = await _unitOfWork.Comments.GetByIdAsync(commentId, cancellationToken);
        if (comment is null || comment.GroupExpenseId != groupExpenseId)
            throw new NotFoundException("Comment not found.", ErrorCodes.CommentNotFound);

        // The author, or the group owner who keeps the conversation tidy.
        var isOwner = group.Members.Any(m => m.UserId == currentUserId && m.Role == GroupRole.Owner);
        if (comment.UserId != currentUserId && !isOwner)
            throw new ForbiddenException(
                "A comment can only be deleted by its author or the group owner.", ErrorCodes.CommentDeleteForbidden);

        _unitOfWork.Comments.Remove(comment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    // ---------- helpers ----------

    /// <summary>
    /// Loads the group (with members) and the expense (with payer and splits), and checks that
    /// the expense belongs to the group and the user is a member of it.
    /// </summary>
    private async Task<(Group Group, GroupExpense Expense)> LoadForMemberAsync(
        Guid groupId,
        Guid groupExpenseId,
        Guid currentUserId,
        CancellationToken cancellationToken)
    {
        var group = await _unitOfWork.Groups.GetWithMembersAsync(groupId, cancellationToken)
            ?? throw new NotFoundException("Group not found.", ErrorCodes.GroupNotFound);

        if (group.Members.All(m => m.UserId != currentUserId))
            throw new ForbiddenException(
                "Only a group member can view or leave comments on group expenses.", ErrorCodes.NotGroupMember);

        // An expense of another group is reported exactly like a missing one.
        var expense = await _unitOfWork.GroupExpenses.GetWithDetailsAsync(groupExpenseId, cancellationToken);
        if (expense is null || expense.GroupId != groupId)
            throw new NotFoundException("Group expense not found.", ErrorCodes.GroupExpenseNotFound);

        return (group, expense);
    }

    /// <summary>
    /// The payer and the participants of the expense who are still members are told in the app
    /// (no e-mail - a conversation would otherwise fill their inbox). The author is skipped.
    /// </summary>
    private async Task NotifyAboutCommentAsync(
        Group group, GroupExpense expense, Comment comment, CancellationToken cancellationToken)
    {
        var memberIds = group.Members.Select(m => m.UserId).ToHashSet();
        var recipients = expense.Splits.Select(s => s.UserId)
            .Append(expense.PaidByUserId)
            .Where(id => id != comment.UserId && memberIds.Contains(id))
            .Distinct();

        var title = string.IsNullOrWhiteSpace(expense.Description) ? expense.Category.Name : expense.Description;
        var text = comment.Text.Length > NotificationQuoteLength
            ? comment.Text[..NotificationQuoteLength].TrimEnd() + "…"
            : comment.Text;

        var message = $"Нови коментар на трошак {NotificationText.Quote(title)} у групи {NotificationText.Quote(group.Name)} " +
                      $"({NotificationText.FullName(comment.User)}): {NotificationText.Quote(text)}";

        foreach (var userId in recipients)
        {
            await _notificationService.NotifyAsync(
                userId,
                NotificationType.GroupExpenseComment,
                "Нови коментар",
                message,
                cancellationToken,
                sendEmail: false);
        }
    }
}