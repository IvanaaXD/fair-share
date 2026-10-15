using FairShare.Domain.Entities;

namespace FairShare.Domain.Interfaces;

public interface ICommentRepository : IRepository<Comment>
{
    /// <summary>Comments of one expense with their authors, oldest first.</summary>
    Task<IReadOnlyList<Comment>> GetByGroupExpenseAsync(
        Guid groupExpenseId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Number of comments per expense, counted in the database (one grouped query).
    /// Expenses without comments are not in the result.
    /// </summary>
    Task<Dictionary<Guid, int>> CountByGroupExpensesAsync(
        IReadOnlyCollection<Guid> groupExpenseIds,
        CancellationToken cancellationToken = default);
}
