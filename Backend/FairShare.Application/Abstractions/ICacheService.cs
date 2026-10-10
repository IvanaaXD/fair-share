namespace FairShare.Application.Abstractions;

/// <summary>
/// Cache used by the application layer. Implemented in Infrastructure with HybridCache: today an
/// in-memory cache inside the API process; registering Redis later turns it into a two-level
/// cache (memory + Redis) without changing any code that uses this interface.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Returns the cached value, or runs <paramref name="factory"/>, caches its result and returns it.
    /// If many requests ask for the same missing key at once, the factory runs only once and
    /// the others wait for its result (protection against a "cache stampede").
    /// </summary>
    /// <param name="tags">Groups of entries that can be invalidated together (see <see cref="InvalidateTagsAsync"/>).</param>
    Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan expiration,
        IReadOnlyCollection<string>? tags = null,
        CancellationToken cancellationToken = default);

    /// <summary>Makes every entry with any of the tags invalid; the next read loads fresh data.</summary>
    Task InvalidateTagsAsync(IEnumerable<string> tags, CancellationToken cancellationToken = default);
}
