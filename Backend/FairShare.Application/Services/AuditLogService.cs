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

        var entries = await _unitOfWork.AuditLogs.SearchAsync(
            userId, entityType, from, to, page, pageSize, cancellationToken);

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
