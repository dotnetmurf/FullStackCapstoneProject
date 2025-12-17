using Microsoft.Extensions.Caching.Memory;

namespace SkillSnap.Api.Services;

/// <summary>
/// Helper service for common caching operations.
/// Provides reusable methods for cache retrieval, storage, and invalidation.
/// </summary>
public class CacheHelper
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CacheHelper> _logger;
    private readonly TimeSpan _defaultSlidingExpiration = TimeSpan.FromMinutes(5);
    private readonly TimeSpan _defaultAbsoluteExpiration = TimeSpan.FromMinutes(10);

    public CacheHelper(IMemoryCache cache, ILogger<CacheHelper> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Attempts to retrieve a value from cache.
    /// </summary>
    /// <typeparam name="T">The type of cached value.</typeparam>
    /// <param name="cacheKey">The cache key to retrieve.</param>
    /// <param name="entityName">The entity name for logging purposes.</param>
    /// <returns>The cached value if found, otherwise null.</returns>
    public T? TryGetFromCache<T>(string cacheKey, string entityName) where T : class
    {
        if (_cache.TryGetValue(cacheKey, out T? cachedValue))
        {
            _logger.LogInformation("{EntityName} retrieved from cache (key: {CacheKey})", entityName, cacheKey);
            return cachedValue;
        }

        return null;
    }

    /// <summary>
    /// Stores a value in cache with default expiration settings.
    /// </summary>
    /// <typeparam name="T">The type of value to cache.</typeparam>
    /// <param name="cacheKey">The cache key to store under.</param>
    /// <param name="value">The value to cache.</param>
    /// <param name="entityName">The entity name for logging purposes.</param>
    public void SetCache<T>(string cacheKey, T value, string entityName)
    {
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(_defaultSlidingExpiration)
            .SetAbsoluteExpiration(_defaultAbsoluteExpiration);

        _cache.Set(cacheKey, value, cacheOptions);
        _logger.LogDebug("{EntityName} cached (key: {CacheKey})", entityName, cacheKey);
    }

    /// <summary>
    /// Stores a value in cache with custom expiration settings.
    /// </summary>
    /// <typeparam name="T">The type of value to cache.</typeparam>
    /// <param name="cacheKey">The cache key to store under.</param>
    /// <param name="value">The value to cache.</param>
    /// <param name="slidingExpiration">Sliding expiration time span.</param>
    /// <param name="absoluteExpiration">Absolute expiration time span.</param>
    /// <param name="entityName">The entity name for logging purposes.</param>
    public void SetCache<T>(string cacheKey, T value, TimeSpan slidingExpiration, TimeSpan absoluteExpiration, string entityName)
    {
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(slidingExpiration)
            .SetAbsoluteExpiration(absoluteExpiration);

        _cache.Set(cacheKey, value, cacheOptions);
        _logger.LogDebug("{EntityName} cached with custom expiration (key: {CacheKey})", entityName, cacheKey);
    }

    /// <summary>
    /// Removes a single cache entry.
    /// </summary>
    /// <param name="cacheKey">The cache key to remove.</param>
    public void RemoveCache(string cacheKey)
    {
        _cache.Remove(cacheKey);
    }

    /// <summary>
    /// Removes multiple cache entries.
    /// </summary>
    /// <param name="cacheKeys">Array of cache keys to remove.</param>
    public void RemoveMultiple(params string[] cacheKeys)
    {
        foreach (var key in cacheKeys)
        {
            _cache.Remove(key);
        }
        _logger.LogDebug("Removed {Count} cache entries", cacheKeys.Length);
    }

    /// <summary>
    /// Invalidates all paginated cache entries using a tracking list.
    /// </summary>
    /// <param name="trackingKey">The key used to store the list of paginated cache keys.</param>
    /// <param name="entityName">The entity name for logging purposes.</param>
    public void InvalidatePagedCaches(string trackingKey, string entityName)
    {
        if (_cache.TryGetValue(trackingKey, out HashSet<string>? pagedKeys) && pagedKeys != null)
        {
            foreach (var key in pagedKeys)
            {
                _cache.Remove(key);
            }
            _logger.LogInformation("Invalidated {Count} paginated {EntityName} cache entries", pagedKeys.Count, entityName);
        }

        // Clear the tracking list itself
        _cache.Remove(trackingKey);
    }

    /// <summary>
    /// Tracks a paginated cache key by adding it to a HashSet.
    /// </summary>
    /// <param name="trackingKey">The key used to store the list of paginated cache keys.</param>
    /// <param name="cacheKey">The cache key to track.</param>
    public void TrackPagedCacheKey(string trackingKey, string cacheKey)
    {
        var pagedKeys = _cache.GetOrCreate(trackingKey, entry =>
        {
            entry.SetSlidingExpiration(TimeSpan.FromMinutes(30));
            entry.SetAbsoluteExpiration(TimeSpan.FromHours(1));
            return new HashSet<string>();
        });

        if (pagedKeys != null)
        {
            pagedKeys.Add(cacheKey);
        }
    }

    /// <summary>
    /// Gets or caches a computed count value.
    /// </summary>
    /// <param name="cacheKey">The cache key for the count.</param>
    /// <param name="computeFunc">Function to compute the count if not cached.</param>
    /// <param name="entityName">The entity name for logging purposes.</param>
    /// <returns>The count value from cache or computed.</returns>
    public async Task<int> GetOrCacheCountAsync(string cacheKey, Func<Task<int>> computeFunc, string entityName)
    {
        if (_cache.TryGetValue(cacheKey, out int cachedCount))
        {
            _logger.LogDebug("{EntityName} count retrieved from cache", entityName);
            return cachedCount;
        }

        var count = await computeFunc();
        
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(10))
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(30));

        _cache.Set(cacheKey, count, cacheOptions);
        _logger.LogDebug("{EntityName} count cached (value: {Count})", entityName, count);
        
        return count;
    }
}
