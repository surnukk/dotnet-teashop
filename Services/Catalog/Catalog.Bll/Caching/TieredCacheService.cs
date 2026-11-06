using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Catalog.Bll.Caching;

/// <summary>Те, що реально лежить у Redis: версія групи, з якою записали, і самі дані.</summary>
public sealed record L2Envelope<T>(string? V, T D);

/// <summary>
/// Дворівневий кеш: L1 (IMemoryCache) → L2 (Redis через IDistributedCache) → БД.
/// Якщо Redis недоступний, сервіс працює лише з L1 і БД.
/// </summary>
public sealed class TieredCacheService(
    IMemoryCache l1,
    IDistributedCache l2,
    ILogger<TieredCacheService> logger) : ICacheService
{
    private static readonly TimeSpan L2CallTimeout = TimeSpan.FromMilliseconds(500);   // довше на Redis не чекаємо
    private static readonly TimeSpan L2RetryAfter = TimeSpan.FromSeconds(15);         // після збою 15 с не чіпаємо Redis
    private static readonly TimeSpan GroupVersionRefresh = TimeSpan.FromSeconds(2);   // як часто звіряємо версію групи
    private static readonly TimeSpan GroupVersionTtl = TimeSpan.FromDays(7);
    private static readonly TimeSpan L1MaxTtl = TimeSpan.FromMinutes(1);

    private sealed record CacheItem(object Value, int SizeBytes, DateTimeOffset ExpiresAt);

    private readonly ConcurrentDictionary<string, CancellationTokenSource> _groups = new();
    private readonly ConcurrentDictionary<string, (string Version, long CheckedAtMs)> _groupVersions = new();

    private long _l1Hits, _l2Hits, _misses, _invalidations, _evictions, _l2Errors;
    private long _l2DownUntilTicks;
    private int _l2WasDown;

    private bool IsL2Down => DateTime.UtcNow.Ticks < Interlocked.Read(ref _l2DownUntilTicks);

    public async Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T?>> factory,
        CacheEntryPolicy policy,
        CancellationToken ct = default) where T : class
    {
        if (l1.TryGetValue(key, out CacheItem? item) && item?.Value is T cached)
        {
            Interlocked.Increment(ref _l1Hits);
            CacheCallContext.Current?.RecordL1Hit();
            logger.LogDebug("Cache L1 hit {Key}: ttl left {TtlSeconds} s, size {SizeBytes} bytes",
                key, Math.Max(0, (int)(item.ExpiresAt - DateTimeOffset.UtcNow).TotalSeconds), item.SizeBytes);
            return cached;
        }

        CancellationToken? groupToken = policy.Group is null ? null : GetGroupSource(policy.Group).Token;
        var groupVersion = policy.Group is null ? null : await GetGroupVersionAsync(policy.Group, ct);

        var fromL2 = await ReadL2Async<T>(key, groupVersion, ct);
        if (fromL2 is { } hit)
        {
            Interlocked.Increment(ref _l2Hits);
            CacheCallContext.Current?.RecordL2Hit();
            logger.LogDebug("Cache L2 hit {Key}: size {SizeBytes} bytes", key, hit.SizeBytes);
            SetL1(key, hit.Value, hit.SizeBytes, policy, groupToken);   // L2 → L1 (заповнення на зворотному шляху)
            return hit.Value;
        }

        Interlocked.Increment(ref _misses);
        CacheCallContext.Current?.RecordMiss();
        logger.LogDebug("Cache miss {Key}", key);

        var value = await factory(ct);
        if (value is null) return null;

        var bytes = Serialize(value, groupVersion);
        SetL1(key, value, bytes?.Length ?? -1, policy, groupToken);
        if (bytes is not null)
            await RunL2Async(token => l2.SetAsync(key, bytes, L2Options(policy), token), ct);

        logger.LogDebug(
            "Cache set {Key}: L1 {L1Seconds} s, L2 {L2Seconds} s, size {SizeBytes} bytes, group {Group}",
            key, (int)L1Ttl(policy).TotalSeconds, (int)policy.Absolute.TotalSeconds, bytes?.Length ?? -1, policy.Group);

        return value;
    }

    public async Task RemoveAsync(string key, string reason, CancellationToken ct = default)
    {
        l1.Remove(key);
        await RunL2Async(token => l2.RemoveAsync(key, token), ct);

        Interlocked.Increment(ref _invalidations);
        logger.LogInformation("Cache invalidated {Key}, reason {Reason}", key, reason);
    }

    public async Task RemoveGroupAsync(string group, string reason, CancellationToken ct = default)
    {
        if (_groups.TryRemove(group, out var source))
            source.Cancel();

        var version = Guid.NewGuid().ToString("N");
        _groupVersions[group] = (version, Environment.TickCount64);
        await RunL2Async(token => l2.SetAsync(
            VersionKey(group),
            Encoding.UTF8.GetBytes(version),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = GroupVersionTtl },
            token), ct);

        Interlocked.Increment(ref _invalidations);
        logger.LogInformation("Cache group invalidated {Group}, reason {Reason}", group, reason);
    }

    public CacheStatistics GetStatistics()
    {
        var stats = (l1 as MemoryCache)?.GetCurrentStatistics();
        return new CacheStatistics(
            Interlocked.Read(ref _l1Hits),
            Interlocked.Read(ref _l2Hits),
            Interlocked.Read(ref _misses),
            Interlocked.Read(ref _invalidations),
            Interlocked.Read(ref _evictions),
            Interlocked.Read(ref _l2Errors),
            !IsL2Down,
            stats?.CurrentEntryCount ?? 0,
            stats?.CurrentEstimatedSize);
    }

    private static TimeSpan L1Ttl(CacheEntryPolicy policy)
    {
        var half = TimeSpan.FromTicks(policy.Absolute.Ticks / 2);
        return half < L1MaxTtl ? half : L1MaxTtl;
    }

    private void SetL1(string key, object value, int sizeBytes, CacheEntryPolicy policy, CancellationToken? groupToken)
    {
        var ttl = L1Ttl(policy);
        var options = new MemoryCacheEntryOptions
        {
            Size = policy.Size,
            Priority = policy.Priority,
            AbsoluteExpirationRelativeToNow = ttl
        };
        if (groupToken is { } token)
            options.AddExpirationToken(new CancellationChangeToken(token));
        options.RegisterPostEvictionCallback(OnEvicted, key);

        l1.Set(key, new CacheItem(value, sizeBytes, DateTimeOffset.UtcNow + ttl), options);
    }

    private void OnEvicted(object key, object? value, EvictionReason reason, object? state)
    {
        if (reason is not (EvictionReason.Capacity or EvictionReason.Expired))
            return;

        Interlocked.Increment(ref _evictions);
        logger.Log(reason == EvictionReason.Capacity ? LogLevel.Information : LogLevel.Debug,
            "Cache entry {Key} evicted from L1, reason {Reason}", key, reason);
    }

    private CancellationTokenSource GetGroupSource(string group)
        => _groups.GetOrAdd(group, _ => new CancellationTokenSource());

    private static string VersionKey(string group) => $"{group}:version";

    private static DistributedCacheEntryOptions L2Options(CacheEntryPolicy policy)
    {
        var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = policy.Absolute };
        if (policy.Sliding is { } sliding && sliding < policy.Absolute)
            options.SlidingExpiration = sliding;
        return options;
    }

    private byte[]? Serialize<T>(T value, string? groupVersion) where T : class
    {
        try { return JsonSerializer.SerializeToUtf8Bytes(new L2Envelope<T>(groupVersion, value)); }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache serialization failed for {Type}, value is not stored in L2", typeof(T).Name);
            return null;
        }
    }

    private async Task<(T Value, int SizeBytes)?> ReadL2Async<T>(string key, string? groupVersion, CancellationToken ct)
        where T : class
    {
        var bytes = await CallL2Async(token => l2.GetAsync(key, token), ct);
        if (bytes is null) return null;

        try
        {
            var envelope = JsonSerializer.Deserialize<L2Envelope<T>>(bytes);
            if (envelope?.D is null) return null;

            if (envelope.V != groupVersion)
            {
                logger.LogDebug("Cache L2 entry {Key} is stale (group version changed), ignored", key);
                return null;
            }

            return (envelope.D, bytes.Length);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Cache L2 entry {Key} is corrupted, ignored", key);
            return null;
        }
    }

    private async Task<string> GetGroupVersionAsync(string group, CancellationToken ct)
    {
        var now = Environment.TickCount64;
        _groupVersions.TryGetValue(group, out var known);
        if (known.Version is not null && now - known.CheckedAtMs < GroupVersionRefresh.TotalMilliseconds)
            return known.Version;

        var bytes = await CallL2Async(token => l2.GetAsync(VersionKey(group), token), ct);
        var version = bytes is { Length: > 0 } ? Encoding.UTF8.GetString(bytes) : known.Version ?? "0";

        _groupVersions[group] = (version, now);
        return version;
    }

    private async Task<TResult?> CallL2Async<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken ct)
    {
        if (IsL2Down) return default;
        try
        {
            var result = await Task.Run(() => operation(ct)).WaitAsync(L2CallTimeout, ct);
            OnL2Success();
            return result;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            OnL2Failure(ex);
            return default;
        }
    }

    private async Task<bool> RunL2Async(Func<CancellationToken, Task> operation, CancellationToken ct)
    {
        if (IsL2Down) return false;
        try
        {
            await Task.Run(() => operation(ct)).WaitAsync(L2CallTimeout, ct);
            OnL2Success();
            return true;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            OnL2Failure(ex);
            return false;
        }
    }

    private void OnL2Failure(Exception ex)
    {
        Interlocked.Increment(ref _l2Errors);
        Interlocked.Exchange(ref _l2DownUntilTicks, (DateTime.UtcNow + L2RetryAfter).Ticks);

        if (Interlocked.Exchange(ref _l2WasDown, 1) == 0)   // лог лише при переході «працює → не працює»
            logger.LogWarning(ex,
                "Redis (L2) unavailable, falling back to L1 + database; next attempt in {RetrySeconds} s",
                (int)L2RetryAfter.TotalSeconds);
    }

    private void OnL2Success()
    {
        if (Interlocked.Exchange(ref _l2WasDown, 0) == 1)
            logger.LogInformation("Redis (L2) is available again");
    }
}