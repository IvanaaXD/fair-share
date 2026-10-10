using FairShare.Application.Abstractions;
using FairShare.Application.Common.Caching;
using FairShare.Application.DTOs.Admin;
using FairShare.Application.DTOs.Analytics;
using FairShare.Application.Interfaces;

namespace FairShare.Application.Services.Caching;

/// <summary>
/// Decorator for the admin statistics: a dozen COUNT queries over every table. Cached briefly
/// (see CacheDurations.AdminStatistics) and not invalidated by tags.
/// </summary>
public class CachedAdminStatisticsService : IAdminStatisticsService
{
    private readonly IAdminStatisticsService _inner;
    private readonly ICacheService _cache;

    public CachedAdminStatisticsService(IAdminStatisticsService inner, ICacheService cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public Task<AdminStatisticsResponse> GetStatisticsAsync(
        AnalyticsPeriod period,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default)
        => _cache.GetOrCreateAsync(
            CacheKeys.AdminStatistics(period, from, to),
            token => _inner.GetStatisticsAsync(period, from, to, token),
            CacheDurations.AdminStatistics,
            cancellationToken: cancellationToken);
}
