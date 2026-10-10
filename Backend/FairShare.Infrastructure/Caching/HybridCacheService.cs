using FairShare.Application.Abstractions;
using Microsoft.Extensions.Caching.Hybrid;

namespace FairShare.Infrastructure.Caching;

/// <summary>
/// ICacheService on top of .NET's HybridCache. Without further configuration it is an in-memory
/// cache (L1). If an IDistributedCache such as Redis is registered, HybridCache uses it as a
/// second level (L2) on its own - see CachingServiceExtensions.
/// </summary>
public sealed class HybridCacheService : ICacheService
{
    private readonly HybridCache _cache;

    public HybridCacheService(HybridCache cache)
    {
        _cache = cache;
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan expiration,
        IReadOnlyCollection<string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        var options = new HybridCacheEntryOptions
        {
            Expiration = expiration,
            LocalCacheExpiration = expiration
        };

        // The factory is passed as "state" so no closure is allocated on every call.
        return await _cache.GetOrCreateAsync<Func<CancellationToken, Task<T>>, T>(
            key,
            factory,
            static async (callFactory, token) => await callFactory(token),
            options,
            tags,
            cancellationToken);
    }

    public async Task InvalidateTagsAsync(IEnumerable<string> tags, CancellationToken cancellationToken = default)
        => await _cache.RemoveByTagAsync(tags, cancellationToken);
}
