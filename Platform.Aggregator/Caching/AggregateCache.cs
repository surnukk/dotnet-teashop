using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace Platform.Aggregator.Caching;

/// <summary>Кеш агрегованих відповідей у Redis (IDistributedCache). Збій Redis не ламає запит: це просто промах.</summary>
public sealed class AggregateCache(IDistributedCache cache, ILogger<AggregateCache> logger)
{
    private static readonly TimeSpan CallTimeout = TimeSpan.FromMilliseconds(500);   // довше на Redis не чекаємо
    private static readonly TimeSpan RetryAfter = TimeSpan.FromSeconds(15);          // після збою 15 с не чіпаємо Redis

    private long _downUntilTicks;

    public static string FullOrderKey(long orderId) => $"aggregator:order:{orderId}:full";

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct) where T : class
    {
        var bytes = await CallAsync(token => cache.GetAsync(key, token), ct);
        if (bytes is null) return null;

        try
        {
            return JsonSerializer.Deserialize<T>(bytes);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Aggregate cache entry {Key} is corrupted, ignored", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value);
        var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl };

        await CallAsync<object?>(async token =>
        {
            await cache.SetAsync(key, bytes, options, token);
            return null;
        }, ct);
    }

    public async Task RemoveAsync(string key, CancellationToken ct)
    {
        await CallAsync<object?>(async token =>
        {
            await cache.RemoveAsync(key, token);
            return null;
        }, ct);

        logger.LogInformation("Aggregate cache invalidated {Key}", key);
    }

    private async Task<TResult?> CallAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken ct)
    {
        if (DateTime.UtcNow.Ticks < Interlocked.Read(ref _downUntilTicks))
            return default;                                    // Redis недавно не відповідав: працюємо без кешу

        try
        {
            return await Task.Run(() => operation(ct)).WaitAsync(CallTimeout, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            Interlocked.Exchange(ref _downUntilTicks, (DateTime.UtcNow + RetryAfter).Ticks);
            logger.LogWarning(ex, "Redis is unavailable, aggregate cache is bypassed for {Seconds} s",
                (int)RetryAfter.TotalSeconds);
            return default;
        }
    }
}