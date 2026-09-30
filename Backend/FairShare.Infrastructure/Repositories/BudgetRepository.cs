using FairShare.Domain.Entities;
using FairShare.Domain.Interfaces;
using FairShare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FairShare.Infrastructure.Repositories;

public class BudgetRepository : GenericRepository<Budget>, IBudgetRepository
{
    public BudgetRepository(FairShareDbContext context) : base(context)
    {
    }

    public async Task<Budget?> GetByUserCategoryAndMonthAsync(
        Guid userId,
        Guid categoryId,
        string month,
        CancellationToken cancellationToken = default)
        => await DbSet.FirstOrDefaultAsync(
            b => b.UserId == userId && b.CategoryId == categoryId && b.Month == month,
            cancellationToken);

    public async Task<IReadOnlyList<Budget>> GetByUserAndMonthAsync(
        Guid userId,
        string month,
        CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking()
            .Where(b => b.UserId == userId && b.Month == month)
            .ToListAsync(cancellationToken);
}
