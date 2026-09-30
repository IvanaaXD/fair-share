using FairShare.Domain.Entities;

namespace FairShare.Domain.Interfaces;

public interface ICategoryRepository : IRepository<Category>
{
    Task<IReadOnlyList<Category>> GetSystemDefinedAsync(CancellationToken cancellationToken = default);

    Task<Category?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
}
