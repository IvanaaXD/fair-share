using FairShare.Domain.Entities;

namespace FairShare.Domain.Interfaces;

public interface IAuditLogRepository : IRepository<AuditLog>
{
    /// <summary>Search and pagination of the audit log for administrator review (feature 5.11).</summary>
    Task<IReadOnlyList<AuditLog>> SearchAsync(
    Guid? userId,
        string? entityType,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
