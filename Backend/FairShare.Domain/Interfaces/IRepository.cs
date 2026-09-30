using FairShare.Domain.Entities;

namespace FairShare.Domain.Interfaces;

/// <summary>
/// Заједничке CRUD операције за све репозиторијуме. Специфични репозиторијуми
/// (нпр. <see cref="IUserRepository"/>) наслеђују овај интерфејс и додају
/// упите специфичне за домен ентитета.
/// </summary>
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);

    Task AddAsync(T entity, CancellationToken cancellationToken = default);

    void Update(T entity);

    void Remove(T entity);
}
