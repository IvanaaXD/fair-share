using FairShare.Domain.Entities;

namespace FairShare.Domain.Interfaces;

public interface IExpenseSplitRepository : IRepository<ExpenseSplit>
{
    Task<IReadOnlyList<ExpenseSplit>> GetByGroupExpenseAsync(
        Guid groupExpenseId,
        CancellationToken cancellationToken = default);

    /// <summary>All user splits within the group - the basis for calculating the net balance.</summary>
    Task<IReadOnlyList<ExpenseSplit>> GetByGroupAndUserAsync(
    Guid groupId,
        Guid userId,
        CancellationToken cancellationToken = default);
}
