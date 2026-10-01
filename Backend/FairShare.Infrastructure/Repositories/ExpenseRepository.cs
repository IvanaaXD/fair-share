using FairShare.Domain.Entities;
using FairShare.Domain.Interfaces;
using FairShare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FairShare.Infrastructure.Repositories;

public class ExpenseRepository : GenericRepository<Expense>, IExpenseRepository
{
    public ExpenseRepository(FairShareDbContext context) : base(context)
    {
    }

    // ИЗМЈЕНА: додат .Include(e => e.Category) - ExpenseService.MapToResponse чита
    // expense.Category.Name директно, без додатног упита по трошку.
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
}
