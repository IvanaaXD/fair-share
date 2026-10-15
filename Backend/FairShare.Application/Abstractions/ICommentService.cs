using FairShare.Application.DTOs.Comments;

namespace FairShare.Application.Interfaces;

public interface ICommentService
{
    /// <summary>
    /// Adds a comment to a group expense. Members only. The payer and the participants of the
    /// expense are notified in the app (not by e-mail).
    /// </summary>
    Task<CommentResponse> CreateAsync(
        Guid groupId,
        Guid groupExpenseId,
        CreateCommentRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Comments of a group expense, oldest first. Members only.</summary>
    Task<IReadOnlyList<CommentResponse>> GetByGroupExpenseAsync(
        Guid groupId,
        Guid groupExpenseId,
        Guid currentUserId,
        CancellationToken cancellationToken = default);

    /// <summary>The author of the comment or the group owner (moderation) can delete it.</summary>
    Task DeleteAsync(
        Guid groupId,
        Guid groupExpenseId,
        Guid commentId,
        Guid currentUserId,
        CancellationToken cancellationToken = default);
}
