using FairShare.Domain.Entities;

namespace FairShare.Domain.Interfaces;

public interface ICategoryRepository : IRepository<Category>
{
    Task<IReadOnlyList<Category>> GetSystemDefinedAsync(CancellationToken cancellationToken = default);

    Task<Category?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    // НОВО: провјерава да ли се категорија користи у трошковима или буџетима, прије брисања.
    Task<bool> IsInUseAsync(Guid categoryId, CancellationToken cancellationToken = default);
}
