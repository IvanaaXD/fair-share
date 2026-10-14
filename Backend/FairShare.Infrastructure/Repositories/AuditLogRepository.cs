using FairShare.Domain.Entities;
using FairShare.Domain.Interfaces;
using FairShare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FairShare.Infrastructure.Repositories;

public class AuditLogRepository : GenericRepository<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(FairShareDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<AuditLog>> SearchAsync(
        Guid? userId,
        string? entityType,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        // Include(User) - the administrator sees the user's name and e-mail in the list.
        var query = DbSet.AsNoTracking().Include(a => a.User).AsQueryable();

        if (userId.HasValue) query = query.Where(a => a.UserId == userId.Value);
        // "Area" filter: the entity type, or the controller part of the action. A sign-in is logged
        // as Action "Auth.Login" with EntityType "User", so "Auth" must also match by action.
        if (!string.IsNullOrWhiteSpace(entityType))
        {
            var actionPrefix = entityType + ".";
            query = query.Where(a => a.EntityType == entityType || a.Action.StartsWith(actionPrefix));
        }
        if (from.HasValue) query = query.Where(a => a.Timestamp >= from.Value);
        if (to.HasValue) query = query.Where(a => a.Timestamp <= to.Value);

        return await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }
}
