using FairShare.Application.Abstractions;
using FairShare.Application.DTOs.Analytics;
using FairShare.Application.DTOs.Reports;
using FairShare.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FairShare.API.Controllers;

/// <summary>Downloadable reports (PDF or CSV) of personal spending and of group debts.</summary>
[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly ICurrentUserService _currentUser;

    public ReportController(IReportService reportService, ICurrentUserService currentUser)
    {
        _reportService = reportService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Personal spending report for the same periods as analytics. Example:
    /// ?format=Pdf&amp;period=Custom&amp;from=2026-09-01&amp;to=2026-09-30&amp;currency=BAM&amp;lang=sr-Latn&amp;timeZone=Europe/Sarajevo
    /// </summary>
    /// <param name="lang">"sr-Cyrl" (default), "sr-Latn" or "en" - the language of the app.</param>
    /// <param name="timeZone">IANA time zone of the user, for the time the report was made.</param>
    [HttpGet("spending")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK, "application/pdf", "text/csv")]
    public async Task<IActionResult> GetSpending(
        [FromQuery] ReportFormat format = ReportFormat.Pdf,
        [FromQuery] AnalyticsPeriod period = AnalyticsPeriod.LastMonth,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string? currency = null,
        [FromQuery] string? lang = null,
        [FromQuery] string? timeZone = null,
        CancellationToken cancellationToken = default)
    {
        var file = await _reportService.GetSpendingReportAsync(
            _currentUser.UserId, period, from, to, currency,
            new ReportRequest(format, ReportLanguages.Parse(lang), timeZone),
            cancellationToken);

        return Download(file);
    }

    /// <summary>Balances, how to settle them, completed settlements and expenses of a group. Members only.</summary>
    [HttpGet("groups/{groupId:guid}")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK, "application/pdf", "text/csv")]
    public async Task<IActionResult> GetGroupDebts(
        [FromRoute] Guid groupId,
        [FromQuery] ReportFormat format = ReportFormat.Pdf,
        [FromQuery] string? lang = null,
        [FromQuery] string? timeZone = null,
        CancellationToken cancellationToken = default)
    {
        var file = await _reportService.GetGroupDebtReportAsync(
            groupId, _currentUser.UserId,
            new ReportRequest(format, ReportLanguages.Parse(lang), timeZone),
            cancellationToken);

        return Download(file);
    }

    private FileContentResult Download(ReportFile file)
    {
        // Reports contain personal financial data: never kept in a browser or proxy cache.
        Response.Headers.CacheControl = "no-store";
        return File(file.Content, file.ContentType, file.FileName);
    }
}
