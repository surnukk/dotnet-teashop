using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Orders.Bll.Caching;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Orders.Bll.Caching;

public sealed class MemoryCacheService(IMemoryCache cache, ILogger<MemoryCacheService> logger) : ICacheService
{
    private sealed record CacheItem(object Value, int SizeBytes, DateTimeOffset ExpiresAt);

    private readonly ConcurrentDictionary<string, CancellationTokenSource> _groups = new();
    private long _hits, _misses, _invalidations, _evictions;

    public async Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T?>> factory,
        CacheEntryPolicy policy,
        CancellationToken ct = default) where T : class
    {
        if (cache.TryGetValue(key, out CacheItem? item) && item?.Value is T cached)
        {
            Interlocked.Increment(ref _hits);
            logger.LogDebug("Cache hit {Key}: ttl left {TtlSeconds} s, size {SizeBytes} bytes",
                key, Math.Max(0, (int)(item.ExpiresAt - DateTimeOffset.UtcNow).TotalSeconds), item.SizeBytes);
            return cached;
        }

        Interlocked.Increment(ref _misses);
        logger.LogDebug("Cache miss {Key}", key);

        CancellationToken? groupToken = policy.Group is null ? null : GetGroupSource(policy.Group).Token;

        var value = await factory(ct);
        if (value is null) return null;

        Set(key, value, policy, groupToken);
        return value;
    }

    public void Remove(string key, string reason)
    {
        cache.Remove(key);
        Interlocked.Increment(ref _invalidations);
        logger.LogInformation("Cache invalidated {Key}, reason {Reason}", key, reason);
    }

    public void RemoveGroup(string group, string reason)
    {
        if (_groups.TryRemove(group, out var source))
            source.Cancel();

        Interlocked.Increment(ref _invalidations);
        logger.LogInformation("Cache group invalidated {Group}, reason {Reason}", group, reason);
    }

    public CacheStatistics GetStatistics()
    {
        var stats = (cache as MemoryCache)?.GetCurrentStatistics();
        return new CacheStatistics(
            Interlocked.Read(ref _hits),
            Interlocked.Read(ref _misses),
            Interlocked.Read(ref _invalidations),
            Interlocked.Read(ref _evictions),
            stats?.CurrentEntryCount ?? 0,
            stats?.CurrentEstimatedSize);
    }

    private void Set(string key, object value, CacheEntryPolicy policy, CancellationToken? groupToken)
    {
        var sizeBytes = EstimateSize(value);

        var options = new MemoryCacheEntryOptions
        {
            Size = policy.Size,
            Priority = policy.Priority,
            AbsoluteExpirationRelativeToNow = policy.Absolute
        };
        if (policy.Sliding is { } sliding)
            options.SlidingExpiration = sliding;
        if (groupToken is { } token)
            options.AddExpirationToken(new CancellationChangeToken(token));
        options.RegisterPostEvictionCallback(OnEvicted, key);

        cache.Set(key, new CacheItem(value, sizeBytes, DateTimeOffset.UtcNow + policy.Absolute), options);

        logger.LogDebug(
            "Cache set {Key}: absolute {AbsoluteSeconds} s, sliding {SlidingSeconds} s, size {SizeBytes} bytes, group {Group}",
            key, (int)policy.Absolute.TotalSeconds, (int?)policy.Sliding?.TotalSeconds, sizeBytes, policy.Group);
    }

    private void OnEvicted(object key, object? value, EvictionReason reason, object? state)
    {
        if (reason is not (EvictionReason.Capacity or EvictionReason.Expired))
            return;

        Interlocked.Increment(ref _evictions);
        logger.Log(reason == EvictionReason.Capacity ? LogLevel.Information : LogLevel.Debug,
            "Cache entry {Key} evicted, reason {Reason}", key, reason);
    }

    private CancellationTokenSource GetGroupSource(string group)
        => _groups.GetOrAdd(group, _ => new CancellationTokenSource());

    private static int EstimateSize(object value)
    {
        try { return JsonSerializer.SerializeToUtf8Bytes(value, value.GetType()).Length; }
        catch { return -1; }
    }
}