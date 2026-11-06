using Microsoft.Extensions.Diagnostics.HealthChecks;
using Orders.Bll.Caching;

namespace Orders.Api.HealthChecks;

public sealed class CacheHealthCheck(ICacheService cache) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        var s = cache.GetStatistics();

        var data = new Dictionary<string, object>
        {
            ["l1Hits"] = s.L1Hits,
            ["l2Hits"] = s.L2Hits,
            ["misses"] = s.Misses,
            ["hitRatio"] = Math.Round(s.HitRatio, 3),
            ["invalidations"] = s.Invalidations,
            ["evictions"] = s.Evictions,
            ["l2Errors"] = s.L2Errors,
            ["l2Available"] = s.L2Available,
            ["entries"] = s.EntryCount,
            ["estimatedSize"] = s.EstimatedSize ?? 0
        };

        return Task.FromResult(HealthCheckResult.Healthy("In-memory cache is operational", data));
    }
}