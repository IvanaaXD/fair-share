using FairShare.Domain.Entities;
using FairShare.Domain.Entities.Enums;
using FairShare.Domain.Interfaces;
using FairShare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FairShare.Infrastructure.Repositories;

public class CategoryRepository : GenericRepository<Category>, ICategoryRepository
{
    public CategoryRepository(FairShareDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Category>> GetSystemDefinedAsync(CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking().Where(c => c.IsSystemDefined).ToListAsync(cancellationToken);

    public async Task<Category?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        => await DbSet.FirstOrDefaultAsync(c => c.Name == name, cancellationToken);

    public async Task<IReadOnlyList<Category>> GetApprovedAsync(CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking()
            .Where(c => c.Status == CategoryStatus.Approved)
            .ToListAsync(cancellationToken);

    public async Task<Category?> GetUsableByIdAsync(Guid categoryId, CancellationToken cancellationToken = default)
        => await DbSet.FirstOrDefaultAsync(
            c => c.Id == categoryId && c.Status == CategoryStatus.Approved, cancellationToken);

    public async Task<Category?> GetActiveByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        // lower() in PostgreSQL: "Ljubimci" and "ljubimci" are the same category.
        var lowered = name.ToLower();
        return await DbSet.FirstOrDefaultAsync(
            c => c.Status != CategoryStatus.Rejected && c.Name.ToLower() == lowered, cancellationToken);
    }

    public async Task<int> CountPendingByUserAsync(Guid userId, CancellationToken cancellationToken = default)
        => await DbSet.CountAsync(
            c => c.ProposedByUserId == userId && c.Status == CategoryStatus.Pending, cancellationToken);

    public async Task<IReadOnlyList<Category>> GetProposalsByUserAsync(Guid userId, CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking()
            .Where(c => c.ProposedByUserId == userId)
            .OrderByDescending(c => c.ProposedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Category>> GetForAdminAsync(CategoryStatus? status, CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking().Include(c => c.ProposedByUser).AsQueryable();
        if (status is not null)
            query = query.Where(c => c.Status == status);

        // Proposals in the order they arrived, the rest alphabetically.
        return await query
            .OrderBy(c => c.Status == CategoryStatus.Pending ? 0 : 1)
            .ThenBy(c => c.ProposedAt)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetUsageCountsAsync(CancellationToken cancellationToken = default)
    {
        // Three grouped queries (GROUP BY CategoryId) instead of loading any rows.
        var expenses = await Context.Set<Expense>()
            .GroupBy(e => e.CategoryId)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var groupExpenses = await Context.Set<GroupExpense>()
            .GroupBy(e => e.CategoryId)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var budgets = await Context.Set<Budget>()
            .GroupBy(b => b.CategoryId)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return expenses.Concat(groupExpenses).Concat(budgets)
            .GroupBy(x => x.CategoryId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Count));
    }

    public async Task<bool> IsInUseAsync(Guid categoryId, CancellationToken cancellationToken = default)
        => await Context.Set<Expense>().AnyAsync(e => e.CategoryId == categoryId, cancellationToken)
           || await Context.Set<GroupExpense>().AnyAsync(ge => ge.CategoryId == categoryId, cancellationToken)
           || await Context.Set<Budget>().AnyAsync(b => b.CategoryId == categoryId, cancellationToken);
}
