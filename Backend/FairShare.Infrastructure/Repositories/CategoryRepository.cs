using FairShare.Domain.Entities;
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

    public async Task<bool> IsInUseAsync(Guid categoryId, CancellationToken cancellationToken = default)
        => await Context.Set<Expense>().AnyAsync(e => e.CategoryId == categoryId, cancellationToken)
           || await Context.Set<GroupExpense>().AnyAsync(ge => ge.CategoryId == categoryId, cancellationToken)
           || await Context.Set<Budget>().AnyAsync(b => b.CategoryId == categoryId, cancellationToken);
}
