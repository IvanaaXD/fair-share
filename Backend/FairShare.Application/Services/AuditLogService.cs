using FairShare.Application.DTOs.Admin;
using FairShare.Application.Interfaces;
using FairShare.Domain.Entities;
using FairShare.Domain.Interfaces;

namespace FairShare.Application.Services;

public class AuditLogService : IAuditLogService
{
    private const int MaxPageSize = 100;

    private readonly IUnitOfWork _unitOfWork;

    public AuditLogService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task LogAsync(
        Guid userId,
        string action,
        string entityType,
        Guid? entityId,
        CancellationToken cancellationToken = default)
    {
        var entry = new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId ?? Guid.Empty,
            Timestamp = DateTime.UtcNow
        };

        await _unitOfWork.AuditLogs.AddAsync(entry, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditLogResponse>> SearchAsync(
        Guid? userId,
        string? entityType,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        // Dates come from the query string as whole days ("2026-10-14") with no time zone.
        // Npgsql only accepts UTC values for timestamp columns, and "to" must include the
        // whole last day, so the range becomes [from 00:00, to 23:59:59.9999999] in UTC.
        var fromUtc = from.HasValue
            ? DateTime.SpecifyKind(from.Value.Date, DateTimeKind.Utc)
            : (DateTime?)null;
        var toUtc = to.HasValue
            ? DateTime.SpecifyKind(to.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc)
            : (DateTime?)null;

        var entries = await _unitOfWork.AuditLogs.SearchAsync(
            userId, entityType, fromUtc, toUtc, page, pageSize, cancellationToken);

        return entries.Select(a => new AuditLogResponse
        {
            Id = a.Id,
            UserId = a.UserId,
            UserFullName = a.User is null ? string.Empty : $"{a.User.FirstName} {a.User.LastName}",
            UserEmail = a.User?.Email ?? string.Empty,
            Action = a.Action,
            EntityType = a.EntityType,
            EntityId = a.EntityId == Guid.Empty ? null : a.EntityId,
            Timestamp = a.Timestamp
        }).ToList();
    }
}
