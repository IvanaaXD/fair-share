using FairShare.Application.Abstractions;
using FairShare.Application.Common.Caching;
using FairShare.Application.DTOs.Analytics;
using FairShare.Application.Interfaces;

namespace FairShare.Application.Services.Caching;

/// <summary>
/// Decorator: the same IAnalyticsService, with caching in front of it. AnalyticsService itself
/// is unchanged and still does all the work on a cache miss. Analytics are the most expensive
/// queries in the system (grouping all expenses of a period) and are opened often, e.g. on the
/// dashboard after every navigation.
/// </summary>
public class CachedAnalyticsService : IAnalyticsService
{
    private readonly IAnalyticsService _inner;
    private readonly ICacheService _cache;

    public CachedAnalyticsService(IAnalyticsService inner, ICacheService cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public Task<SpendingAnalyticsResponse> GetSpendingAnalyticsAsync(
        Guid currentUserId,
        AnalyticsPeriod period,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default)
        => _cache.GetOrCreateAsync(
            CacheKeys.SpendingAnalytics(currentUserId, period, from, to),
            token => _inner.GetSpendingAnalyticsAsync(currentUserId, period, from, to, token),
            CacheDurations.Analytics,
            Tags(currentUserId),
            cancellationToken);

    public Task<IReadOnlyList<ExpenseLocationResponse>> GetExpenseLocationsAsync(
        Guid currentUserId,
        AnalyticsPeriod period,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default)
        => _cache.GetOrCreateAsync(
            CacheKeys.ExpenseLocations(currentUserId, period, from, to),
            token => _inner.GetExpenseLocationsAsync(currentUserId, period, from, to, token),
            CacheDurations.Analytics,
            Tags(currentUserId),
            cancellationToken);

    // Outdated when the user's expenses change, or when a category is renamed.
    private static string[] Tags(Guid userId) => new[] { CacheTags.UserSpending(userId), CacheTags.Categories };
}
