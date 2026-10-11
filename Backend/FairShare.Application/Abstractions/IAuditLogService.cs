using FairShare.Application.DTOs.Admin;

namespace FairShare.Application.Interfaces;

public interface IAuditLogService
{
    /// <summary>Logs a single user action. It is called automatically by AuditLogFilter, and for logins by IdentityService.</summary>
    Task LogAsync(
        Guid userId,
        string action,
        string entityType,
        Guid? entityId,
        CancellationToken cancellationToken = default);

    /// <summary>Audit log search for administrators (feature 5.11).</summary>
    Task<IReadOnlyList<AuditLogResponse>> SearchAsync(
        Guid? userId,
        string? entityType,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
