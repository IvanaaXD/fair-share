using FairShare.Domain.Entities;

namespace FairShare.Domain.Interfaces;

public interface ICommentRepository : IRepository<Comment>
{
    Task<IReadOnlyList<Comment>> GetByGroupExpenseAsync(
        Guid groupExpenseId,
        CancellationToken cancellationToken = default);
}
