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
}
