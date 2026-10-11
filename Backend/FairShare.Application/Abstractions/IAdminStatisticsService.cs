using FairShare.Application.DTOs.Admin;
using FairShare.Application.DTOs.Analytics;

namespace FairShare.Application.Interfaces;

public interface IAdminStatisticsService
{
    /// <summary>Overall system status and the number of new records over time </summary>
    Task<AdminStatisticsResponse> GetStatisticsAsync(
    AnalyticsPeriod period,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default);
}
