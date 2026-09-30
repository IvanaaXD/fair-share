using FairShare.Domain.Entities;
using FairShare.Domain.Interfaces;
using FairShare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FairShare.Infrastructure.Repositories;

/// <summary>
/// Основна EF Core имплементација заједничких CRUD операција. Специфични
/// репозиторијуми наслеђују ову класу и додају упите специфичне за ентитет.
/// </summary>
public class GenericRepository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly FairShareDbContext Context;
    protected readonly DbSet<T> DbSet;

    public GenericRepository(FairShareDbContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await DbSet.FindAsync(new object[] { id }, cancellationToken);

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking().ToListAsync(cancellationToken);

    public virtual async Task AddAsync(T entity, CancellationToken cancellationToken = default)
        => await DbSet.AddAsync(entity, cancellationToken);

    public virtual void Update(T entity) => DbSet.Update(entity);

    public virtual void Remove(T entity) => DbSet.Remove(entity);
}
