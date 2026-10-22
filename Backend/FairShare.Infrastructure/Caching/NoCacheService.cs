using FairShare.Application.Abstractions;

namespace FairShare.Infrastructure.Caching;

/// <summary>
/// ICacheService that caches nothing: every call runs the factory. Used only when
/// "Caching:Enabled" is false - to measure with a load test how much the cache really helps.
/// </summary>
public sealed class NoCacheService : ICacheService
{
    public Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan expiration,
        IReadOnlyCollection<string>? tags = null,
        CancellationToken cancellationToken = default)
        => factory(cancellationToken);

    public Task InvalidateTagsAsync(IEnumerable<string> tags, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
