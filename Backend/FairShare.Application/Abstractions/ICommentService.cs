using FairShare.Application.DTOs.Comments;

namespace FairShare.Application.Interfaces;

public interface ICommentService
{
    Task<CommentResponse> CreateAsync(
        Guid groupId,
        Guid groupExpenseId,
        CreateCommentRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CommentResponse>> GetByGroupExpenseAsync(
        Guid groupId,
        Guid groupExpenseId,
        Guid currentUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Само аутор коментара може да га обрише.</summary>
    Task DeleteAsync(Guid commentId, Guid currentUserId, CancellationToken cancellationToken = default);
}
