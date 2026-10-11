using FairShare.Domain.Entities;

namespace FairShare.Domain.Interfaces;

/// <summary>
/// Common CRUD operations for all repositories. Specific repositories
/// (e.g., <see cref="IUserRepository"/>) inherit this interface and add
/// queries specific to the entity domain.
/// </summary>
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);

    Task AddAsync(T entity, CancellationToken cancellationToken = default);

    void Update(T entity);

    void Remove(T entity);
}
