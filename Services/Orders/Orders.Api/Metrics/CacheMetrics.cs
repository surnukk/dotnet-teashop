using Microsoft.Extensions.Caching.Memory;
using Orders.Bll.Caching;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Orders.Api.Metrics;

public sealed class CacheMetrics : ICacheMetrics
{
    public const string MeterName = "Orders.Cache";   

    private readonly Counter<long> _hits;
    private readonly Counter<long> _misses;
    private readonly Counter<long> _invalidations;
    private readonly Histogram<double> _duration;

    public CacheMetrics(IMeterFactory meterFactory, IMemoryCache memoryCache)
    {
        var meter = meterFactory.Create(MeterName);

        _hits = meter.CreateCounter<long>("cache.hits", description: "Cache hits by level");
        _misses = meter.CreateCounter<long>("cache.misses", description: "Cache misses by level");
        _invalidations = meter.CreateCounter<long>("cache.invalidations", description: "Cache invalidations");
        _duration = meter.CreateHistogram<double>("cache.operation.duration", unit: "ms",
            description: "Cache operation duration");

        meter.CreateObservableGauge<long>("cache.memory.entries",
            () => memoryCache.GetCurrentStatistics()?.CurrentEntryCount ?? 0,
            description: "Entries in memory cache");
        meter.CreateObservableGauge<long>("cache.memory.size",
            () => memoryCache.GetCurrentStatistics()?.CurrentEstimatedSize ?? 0,
            description: "Estimated size of memory cache (SizeLimit units)");
    }

    public void L1Hit() => _hits.Add(1, Level("memory"));

    public void L2Hit()
    {
        _misses.Add(1, Level("memory"));     
        _hits.Add(1, Level("redis"));      
    }

    public void FullMiss()
    {
        _misses.Add(1, Level("memory"));
        _misses.Add(1, Level("redis"));
    }

    public void Invalidated(string scope) =>
        _invalidations.Add(1, new KeyValuePair<string, object?>("cache.scope", scope));

    public IDisposable Time(string operation) => new TimerScope(_duration, operation);

    private static KeyValuePair<string, object?> Level(string level) => new("cache.level", level);

    private sealed class TimerScope(Histogram<double> histogram, string operation) : IDisposable
    {
        private readonly long _start = Stopwatch.GetTimestamp();

        public void Dispose() =>
            histogram.Record(Stopwatch.GetElapsedTime(_start).TotalMilliseconds,
                new KeyValuePair<string, object?>("cache.operation", operation));
    }
}