using FairShare.Application.DTOs.Analytics;

namespace FairShare.Application.Interfaces;

public interface IAnalyticsService
{
    /// <summary>Total expenses over time, by category, and comparison with the previous period.</summary>
    Task<SpendingAnalyticsResponse> GetSpendingAnalyticsAsync(
    Guid currentUserId,
        AnalyticsPeriod period,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default);

    /// <summary>Expenses with location in the period, for map display.</summary>
    Task<IReadOnlyList<ExpenseLocationResponse>> GetExpenseLocationsAsync(
    Guid currentUserId,
        AnalyticsPeriod period,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default);
}
