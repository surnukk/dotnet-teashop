using Microsoft.Extensions.Caching.Memory;

namespace Reviews.Application.Common.Caching;

public sealed record CacheEntryPolicy(
    TimeSpan Absolute,
    TimeSpan? Sliding = null,
    string? Group = null,
    CacheItemPriority Priority = CacheItemPriority.Normal,
    int Size = 1);

public sealed record CacheStatistics(
    long L1Hits,
    long L2Hits,
    long Misses,
    long Invalidations,
    long Evictions,
    long L2Errors,
    bool L2Available,
    long EntryCount,
    long? EstimatedSize)
{
    public long Hits => L1Hits + L2Hits;
    public double HitRatio => Hits + Misses == 0 ? 0 : (double)Hits / (Hits + Misses);
}

public interface ICacheService
{
    Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T?>> factory,
        CacheEntryPolicy policy,
        CancellationToken ct = default) where T : class;

    Task RemoveAsync(string key, string reason, CancellationToken ct = default);
    Task RemoveGroupAsync(string group, string reason, CancellationToken ct = default);
    CacheStatistics GetStatistics();
}