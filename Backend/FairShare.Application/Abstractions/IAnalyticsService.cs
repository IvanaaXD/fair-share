using FairShare.Application.DTOs.Analytics;

namespace FairShare.Application.Interfaces;

public interface IAnalyticsService
{
    /// <summary>
    /// Total spending over time, by category and compared with the previous period.
    /// When <paramref name="currency"/> is given, only expenses in that currency are counted.
    /// </summary>
    Task<SpendingAnalyticsResponse> GetSpendingAnalyticsAsync(
        Guid currentUserId,
        AnalyticsPeriod period,
        DateTime? from,
        DateTime? to,
        string? currency,
        CancellationToken cancellationToken = default);

    /// <summary>Expenses with a location in the period, for the map.</summary>
    Task<IReadOnlyList<ExpenseLocationResponse>> GetExpenseLocationsAsync(
        Guid currentUserId,
        AnalyticsPeriod period,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default);
}
