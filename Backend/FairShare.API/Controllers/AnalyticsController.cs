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
    /// Personal expense analytics. Example: ?period=Last3Months&amp;currency=BAM or
    /// ?period=Custom&amp;from=2026-01-01&amp;to=2026-03-31&amp;currency=BAM
    /// </summary>
    /// <param name="currency">
    /// Optional ISO code. When given, only expenses in that currency are counted, so amounts in
    /// different currencies are never added together. The web app sends the user's default currency.
    /// </param>
    [HttpGet("spending")]
    public async Task<ActionResult<SpendingAnalyticsResponse>> GetSpending(
        [FromQuery] AnalyticsPeriod period = AnalyticsPeriod.LastMonth,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string? currency = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _analyticsService.GetSpendingAnalyticsAsync(
            _currentUser.UserId, period, from, to, currency, cancellationToken);
        return Ok(result);
    }

    /// <summary>Expenses with location for map display (up to 500 most recent), in every currency.</summary>
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
