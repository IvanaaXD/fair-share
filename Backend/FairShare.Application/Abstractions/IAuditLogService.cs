using FairShare.Application.DTOs.Admin;

namespace FairShare.Application.Interfaces;

public interface IAuditLogService
{
    /// <summary>Биљежи једну акцију корисника. Позива га AuditLogFilter аутоматски, а пријаву IdentityService.</summary>
    Task LogAsync(
        Guid userId,
        string action,
        string entityType,
        Guid? entityId,
        CancellationToken cancellationToken = default);

    /// <summary>Претрага ревизионог дневника за администратора (функционалност 5.11).</summary>
    Task<IReadOnlyList<AuditLogResponse>> SearchAsync(
        Guid? userId,
        string? entityType,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
