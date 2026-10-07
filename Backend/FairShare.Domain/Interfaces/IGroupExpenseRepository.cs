using FairShare.Domain.Entities;

namespace FairShare.Domain.Interfaces;

public interface IGroupExpenseRepository : IRepository<GroupExpense>
{
    Task<GroupExpense?> GetWithSplitsAsync(Guid groupExpenseId, CancellationToken cancellationToken = default);

    // NEW: tracked expense with category, payer and splits (with users) - everything needed
    // to edit the expense and to map it to a response without further queries.
    Task<GroupExpense?> GetWithDetailsAsync(Guid groupExpenseId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GroupExpense>> GetByGroupAsync(
        Guid groupId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>All expenses of the group (no paging) with splits - used to calculate balances.</summary>
    Task<IReadOnlyList<GroupExpense>> GetAllByGroupAsync(
        Guid groupId,
        CancellationToken cancellationToken = default);
}
