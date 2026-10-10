using FairShare.Domain.Entities;
using FairShare.Domain.Interfaces;
using FairShare.Domain.Models;
using FairShare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FairShare.Infrastructure.Repositories;

public class ExpenseRepository : GenericRepository<Expense>, IExpenseRepository
{
    // Escape character for LIKE patterns, so "%" and "_" typed by the user are searched literally.
    private const string LikeEscape = "\\";

    public ExpenseRepository(FairShareDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Expense>> GetByUserAsync(
        Guid userId,
        DateTime? from = null,
        DateTime? to = null,
        Guid? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking()
            .Include(e => e.Category)
            .Where(e => e.UserId == userId);

        if (from.HasValue) query = query.Where(e => e.Date >= from.Value);
        if (to.HasValue) query = query.Where(e => e.Date <= to.Value);
        if (categoryId.HasValue) query = query.Where(e => e.CategoryId == categoryId.Value);

        return await query.OrderByDescending(e => e.Date).ToListAsync(cancellationToken);
    }

    // ---------- NEW: search with paging ----------

    public async Task<PagedResult<Expense>> GetPagedByUserAsync(
        ExpenseFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking()
            .Include(e => e.Category)
            .Where(e => e.UserId == filter.UserId);

        if (filter.From.HasValue) query = query.Where(e => e.Date >= filter.From.Value);
        if (filter.ToExclusive.HasValue) query = query.Where(e => e.Date < filter.ToExclusive.Value);
        if (filter.CategoryId.HasValue) query = query.Where(e => e.CategoryId == filter.CategoryId.Value);
        if (filter.MinAmount.HasValue) query = query.Where(e => e.Amount >= filter.MinAmount.Value);
        if (filter.MaxAmount.HasValue) query = query.Where(e => e.Amount <= filter.MaxAmount.Value);
        if (filter.IsRecurring.HasValue) query = query.Where(e => e.IsRecurring == filter.IsRecurring.Value);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            // ILIKE is PostgreSQL's case-insensitive LIKE: "STAN" finds "stan" and "Стан" finds "стан".
            // Latin and Cyrillic are different letters, so "stan" does not find "стан".
            // The text is sent as a query parameter, never concatenated into the SQL.
            var pattern = $"%{EscapeLikePattern(filter.Search.Trim())}%";
            query = query.Where(e =>
                (e.Description != null && EF.Functions.ILike(e.Description, pattern, LikeEscape)) ||
                EF.Functions.ILike(e.Category.Name, pattern, LikeEscape));
        }

        // Counted before paging: the frontend needs the total to show the number of pages.
        var totalCount = await query.CountAsync(cancellationToken);

        var ordered = (filter.SortBy, filter.SortDescending) switch
        {
            (ExpenseSortField.Amount, true) => query.OrderByDescending(e => e.Amount).ThenByDescending(e => e.Date),
            (ExpenseSortField.Amount, false) => query.OrderBy(e => e.Amount).ThenBy(e => e.Date),
            (_, false) => query.OrderBy(e => e.Date),
            _ => query.OrderByDescending(e => e.Date)
        };

        // The Id as the last sort key makes the order stable: two expenses with the same date
        // can never swap places between pages (and appear twice or not at all).
        var items = await ordered
            .ThenBy(e => e.Id)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Expense>(items, totalCount);
    }

    public async Task<Expense?> GetWithCategoryAsync(Guid expenseId, CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking()
            .Include(e => e.Category)
            .FirstOrDefaultAsync(e => e.Id == expenseId, cancellationToken);

    public async Task<decimal> GetTotalByUserAndCategoryAsync(
        Guid userId,
        Guid categoryId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking()
            .Where(e => e.UserId == userId && e.CategoryId == categoryId && e.Date >= from && e.Date <= to)
            .SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0m;

    // CHANGED: due by NextOccurrenceDate; also returns recurring expenses that have no date yet
    // (created before this feature), so the background job can schedule them.
    public async Task<IReadOnlyList<Guid>> GetRecurringDueIdsAsync(
        DateTime asOf,
        CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking()
            .Where(e => e.IsRecurring && (e.NextOccurrenceDate == null || e.NextOccurrenceDate <= asOf))
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);

    // ---------- analytics aggregates ----------

    public async Task<IReadOnlyList<DailySpendingTotal>> GetDailyTotalsAsync(
        Guid userId,
        DateTime from,
        DateTime toExclusive,
        CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking()
            .Where(e => e.UserId == userId && e.Date >= from && e.Date < toExclusive)
            .GroupBy(e => e.Date.Date)
            .Select(g => new DailySpendingTotal(g.Key, g.Sum(e => e.Amount), g.Count()))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CategorySpendingTotal>> GetTotalsByCategoryAsync(
        Guid userId,
        DateTime from,
        DateTime toExclusive,
        CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking()
            .Where(e => e.UserId == userId && e.Date >= from && e.Date < toExclusive)
            .GroupBy(e => new { e.CategoryId, e.Category.Name })
            .Select(g => new CategorySpendingTotal(g.Key.CategoryId, g.Key.Name, g.Sum(e => e.Amount), g.Count()))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ExpenseLocation>> GetLocationsAsync(
        Guid userId,
        DateTime from,
        DateTime toExclusive,
        int maxResults,
        CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking()
            .Where(e => e.UserId == userId
                        && e.Date >= from && e.Date < toExclusive
                        && e.Latitude != null && e.Longitude != null)
            .OrderByDescending(e => e.Date)
            .Take(maxResults)
            .Select(e => new ExpenseLocation(
                e.Id, e.Amount, e.Currency, e.Date, e.Description, e.Category.Name,
                e.Latitude!.Value, e.Longitude!.Value))
            .ToListAsync(cancellationToken);

    /// <summary>Makes \, % and _ match themselves instead of acting as LIKE wildcards.</summary>
    private static string EscapeLikePattern(string text)
        => text
            .Replace(LikeEscape, LikeEscape + LikeEscape)
            .Replace("%", LikeEscape + "%")
            .Replace("_", LikeEscape + "_");
}
