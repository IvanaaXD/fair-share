using FairShare.Application.DTOs.Analytics;
using FairShare.Application.DTOs.Reports;

namespace FairShare.Application.Interfaces;

/// <param name="TimeZone">IANA id of the user's time zone, for the "generated at" time.</param>
public sealed record ReportRequest(ReportFormat Format, ReportLanguage Language, string? TimeZone);

public interface IReportService
{
    /// <summary>
    /// Personal spending in a period (the same periods as analytics) with every expense,
    /// as PDF or CSV. All amounts are converted to <paramref name="currency"/> or, if it is not
    /// given, to the user's default currency.
    /// </summary>
    Task<ReportFile> GetSpendingReportAsync(
        Guid currentUserId,
        AnalyticsPeriod period,
        DateTime? from,
        DateTime? to,
        string? currency,
        ReportRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Balances, how to settle them, completed settlements and expenses of a group. Members only.</summary>
    Task<ReportFile> GetGroupDebtReportAsync(
        Guid groupId,
        Guid currentUserId,
        ReportRequest request,
        CancellationToken cancellationToken = default);
}
