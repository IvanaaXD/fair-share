using FairShare.Application.DTOs.Analytics;

namespace FairShare.Application.Interfaces;

public interface IAnalyticsService
{
    /// <summary>Укупна потрошња кроз вријеме, по категоријама и поређење са претходним периодом.</summary>
    Task<SpendingAnalyticsResponse> GetSpendingAnalyticsAsync(
        Guid currentUserId,
        AnalyticsPeriod period,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default);

    /// <summary>Трошкови са локацијом у периоду, за приказ на мапи.</summary>
    Task<IReadOnlyList<ExpenseLocationResponse>> GetExpenseLocationsAsync(
        Guid currentUserId,
        AnalyticsPeriod period,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default);
}
