using FairShare.Application.DTOs.Admin;
using FairShare.Application.DTOs.Analytics;

namespace FairShare.Application.Interfaces;

public interface IAdminStatisticsService
{
    /// <summary>Укупно стање система и број нових записа кроз вријеме (функционалност 5.11).</summary>
    Task<AdminStatisticsResponse> GetStatisticsAsync(
        AnalyticsPeriod period,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default);
}
