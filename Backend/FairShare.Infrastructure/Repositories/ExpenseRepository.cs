using FairShare.Domain.Entities;
using FairShare.Domain.Interfaces;
using FairShare.Domain.Models;
using FairShare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FairShare.Infrastructure.Repositories;

public class ExpenseRepository : GenericRepository<Expense>, IExpenseRepository
{
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

    public async Task<decimal> GetTotalByUserAndCategoryAsync(
        Guid userId,
        Guid categoryId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking()
            .Where(e => e.UserId == userId && e.CategoryId == categoryId && e.Date >= from && e.Date <= to)
            .SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0m;

    public async Task<IReadOnlyList<Expense>> GetRecurringDueAsync(
        DateTime asOf,
        CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking()
            .Where(e => e.IsRecurring && e.Date <= asOf)
            .ToListAsync(cancellationToken);

    // ---------- НОВО: агрегације за аналитику ----------

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
}
