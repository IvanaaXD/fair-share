using FairShare.Application.DTOs.Comments;
using FairShare.Application.Interfaces;
using FairShare.Domain.Entities;
using FairShare.Domain.Interfaces;
using TimeSheet.Application.Common.Exceptions;

namespace FairShare.Application.Services;

public class CommentService : ICommentService
{
    private readonly IUnitOfWork _unitOfWork;

    public CommentService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CommentResponse> CreateAsync(
        Guid groupId,
        Guid groupExpenseId,
        CreateCommentRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            throw new ConflictException("Текст коментара не смије бити празан.");

        var groupExpense = await ValidateGroupExpenseAndMembershipAsync(
            groupId, groupExpenseId, currentUserId, cancellationToken);

        var author = await _unitOfWork.Users.GetByIdAsync(currentUserId, cancellationToken)
            ?? throw new NotFoundException("Корисник није пронађен.");

        var comment = new Comment
        {
            Text = request.Text.Trim(),
            CreatedAt = DateTime.UtcNow,
            UserId = currentUserId,
            User = author,
            GroupExpenseId = groupExpense.Id
        };

        await _unitOfWork.Comments.AddAsync(comment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToResponse(comment);
    }

    public async Task<IReadOnlyList<CommentResponse>> GetByGroupExpenseAsync(
        Guid groupId,
        Guid groupExpenseId,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        await ValidateGroupExpenseAndMembershipAsync(groupId, groupExpenseId, currentUserId, cancellationToken);

        var comments = await _unitOfWork.Comments.GetByGroupExpenseAsync(groupExpenseId, cancellationToken);
        return comments.Select(MapToResponse).ToList();
    }

    public async Task DeleteAsync(Guid commentId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var comment = await _unitOfWork.Comments.GetByIdAsync(commentId, cancellationToken)
            ?? throw new NotFoundException("Коментар није пронађен.");

        if (comment.UserId != currentUserId)
            throw new ForbiddenException("Можете обрисати само сопствени коментар.");

        _unitOfWork.Comments.Remove(comment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    // ---------- помоћне методе ----------

    private async Task<GroupExpense> ValidateGroupExpenseAndMembershipAsync(
        Guid groupId,
        Guid groupExpenseId,
        Guid currentUserId,
        CancellationToken cancellationToken)
    {
        var groupExpense = await _unitOfWork.GroupExpenses.GetByIdAsync(groupExpenseId, cancellationToken)
            ?? throw new NotFoundException("Групни трошак није пронађен.");

        if (groupExpense.GroupId != groupId)
            throw new NotFoundException("Групни трошак не припада наведеној групи.");

        if (!await _unitOfWork.GroupMembers.IsMemberAsync(groupId, currentUserId, cancellationToken))
            throw new ForbiddenException("Само члан групе може видјети или остављати коментаре на групне трошкове.");

        return groupExpense;
    }

    private static CommentResponse MapToResponse(Comment comment) => new()
    {
        Id = comment.Id,
        Text = comment.Text,
        CreatedAt = comment.CreatedAt,
        UserId = comment.UserId,
        FirstName = comment.User.FirstName,
        LastName = comment.User.LastName,
        GroupExpenseId = comment.GroupExpenseId
    };
}
