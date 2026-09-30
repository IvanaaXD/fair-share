using FairShare.Domain.Entities;

namespace FairShare.Domain.Interfaces;

public interface IAuditLogRepository : IRepository<AuditLog>
{
    /// <summary>Претрага и пагинација ревизионог дневника за администраторски преглед (функционалност 5.11).</summary>
    Task<IReadOnlyList<AuditLog>> SearchAsync(
        Guid? userId,
        string? entityType,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
