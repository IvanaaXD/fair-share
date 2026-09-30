using FairShare.Domain.Entities;

namespace FairShare.Domain.Interfaces;

public interface IExpenseSplitRepository : IRepository<ExpenseSplit>
{
    Task<IReadOnlyList<ExpenseSplit>> GetByGroupExpenseAsync(
        Guid groupExpenseId,
        CancellationToken cancellationToken = default);

    /// <summary>Све подјеле корисника унутар групе - основа за рачунање нето салда.</summary>
    Task<IReadOnlyList<ExpenseSplit>> GetByGroupAndUserAsync(
        Guid groupId,
        Guid userId,
        CancellationToken cancellationToken = default);
}
