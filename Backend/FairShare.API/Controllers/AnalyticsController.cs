using FairShare.Application.Abstractions;
using FairShare.Application.DTOs.Analytics;
using FairShare.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FairShare.API.Controllers;

[ApiController]
[Route("api/analytics")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;
    private readonly ICurrentUserService _currentUser;

    public AnalyticsController(IAnalyticsService analyticsService, ICurrentUserService currentUser)
    {
        _analyticsService = analyticsService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Аналитика личне потрошње. Примјер: ?period=Last3Months или
    /// ?period=Custom&amp;from=2026-01-01&amp;to=2026-03-31
    /// </summary>
    [HttpGet("spending")]
    public async Task<ActionResult<SpendingAnalyticsResponse>> GetSpending(
        [FromQuery] AnalyticsPeriod period = AnalyticsPeriod.LastMonth,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _analyticsService.GetSpendingAnalyticsAsync(
            _currentUser.UserId, period, from, to, cancellationToken);
        return Ok(result);
    }

    /// <summary>Трошкови са локацијом за приказ на мапи (највише 500 најновијих).</summary>
    [HttpGet("map")]
    public async Task<ActionResult<IReadOnlyList<ExpenseLocationResponse>>> GetMap(
        [FromQuery] AnalyticsPeriod period = AnalyticsPeriod.LastMonth,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _analyticsService.GetExpenseLocationsAsync(
            _currentUser.UserId, period, from, to, cancellationToken);
        return Ok(result);
    }
}
