using Microsoft.Extensions.Caching.Memory;

namespace Catalog.Bll.Caching;

public sealed record CacheEntryPolicy(
    TimeSpan Absolute,
    TimeSpan? Sliding = null,
    string? Group = null,
    CacheItemPriority Priority = CacheItemPriority.Normal,
    int Size = 1);

public sealed record CacheStatistics(
    long Hits, long Misses, long Invalidations, long Evictions, long EntryCount, long? EstimatedSize)
{
    public double HitRatio => Hits + Misses == 0 ? 0 : (double)Hits / (Hits + Misses);
}

public interface ICacheService
{
    Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T?>> factory,
        CacheEntryPolicy policy,
        CancellationToken ct = default) where T : class;

    void Remove(string key, string reason);
    void RemoveGroup(string group, string reason);
    CacheStatistics GetStatistics();
}