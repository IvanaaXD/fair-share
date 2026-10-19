using FairShare.Application.DTOs.Admin;
using FairShare.Application.DTOs.Analytics;
using FairShare.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FairShare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminStatisticsService _statisticsService;
    private readonly IAuditLogService _auditLogService;

    public AdminController(IAdminStatisticsService statisticsService, IAuditLogService auditLogService)
    {
        _statisticsService = statisticsService;
        _auditLogService = auditLogService;
    }

    /// <summary>System statistics: overall status and new records over time. Example: ?period=Last3Months</summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<AdminStatisticsResponse>> GetStatistics(
        [FromQuery] AnalyticsPeriod period = AnalyticsPeriod.LastMonth,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _statisticsService.GetStatisticsAsync(period, from, to, cancellationToken);
        return Ok(result);
    }

    /// <summary>Audit log search, newest records first.</summary>
    [HttpGet("audit-logs")]
    public async Task<ActionResult<IReadOnlyList<AuditLogResponse>>> GetAuditLogs(
        [FromQuery] Guid? userId,
        [FromQuery] string? entityType,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _auditLogService.SearchAsync(userId, entityType, from, to, page, pageSize, cancellationToken);
        return Ok(result);
    }
}
