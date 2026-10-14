using FairShare.Domain.Entities;
using FairShare.Domain.Models;

namespace FairShare.Domain.Interfaces;

public interface IExpenseRepository : IRepository<Expense>
{
    /// <summary>Filtering of personal expenses without paging (kept for existing callers).</summary>
    Task<IReadOnlyList<Expense>> GetByUserAsync(
        Guid userId,
        DateTime? from = null,
        DateTime? to = null,
        Guid? categoryId = null,
        CancellationToken cancellationToken = default);

    /// <summary>Search, filtering, sorting and paging of a user's expenses (with category).</summary>
    Task<PagedResult<Expense>> GetPagedByUserAsync(ExpenseFilter filter, CancellationToken cancellationToken = default);

    /// <summary>One expense with its category, not tracked (for reading only).</summary>
    Task<Expense?> GetWithCategoryAsync(Guid expenseId, CancellationToken cancellationToken = default);

    /// <summary>Spending of a user in a category within a period, all currencies added as they are (kept for existing callers).</summary>
    Task<decimal> GetTotalByUserAndCategoryAsync(
        Guid userId,
        Guid categoryId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Spending of a user in a category within a period (both ends included), one row per
    /// currency - budgets convert them to the user's currency instead of adding KM and EUR.
    /// </summary>
    Task<IReadOnlyList<CurrencyTotal>> GetTotalsByCurrencyAsync(
        Guid userId,
        Guid categoryId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ids of recurring expenses whose next copy is due (or not scheduled yet).
    /// Only ids - each expense is then processed in its own scope by the background job.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetRecurringDueIdsAsync(DateTime asOf, CancellationToken cancellationToken = default);

    // ---------- analytics aggregates ----------
    // All periods are [from, toExclusive) - start included, end excluded.
    // Rows are grouped by currency too; AnalyticsService converts them to one currency.

    /// <summary>Total spending per day and currency; grouping is done in the database, not in memory.</summary>
    Task<IReadOnlyList<DailySpendingTotal>> GetDailyTotalsAsync(
        Guid userId,
        DateTime from,
        DateTime toExclusive,
        string? currency = null,
        CancellationToken cancellationToken = default);

    /// <summary>Total spending per category and currency in the period.</summary>
    Task<IReadOnlyList<CategorySpendingTotal>> GetTotalsByCategoryAsync(
        Guid userId,
        DateTime from,
        DateTime toExclusive,
        string? currency = null,
        CancellationToken cancellationToken = default);

    /// <summary>Newest expenses with a location in the period, for the map.</summary>
    Task<IReadOnlyList<ExpenseLocation>> GetLocationsAsync(
        Guid userId,
        DateTime from,
        DateTime toExclusive,
        int maxResults,
        CancellationToken cancellationToken = default);
}
