using Catalog.Bll.Caching;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Catalog.Api.HealthChecks;

public sealed class CacheHealthCheck(ICacheService cache) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        var s = cache.GetStatistics();

        var data = new Dictionary<string, object>
        {
            ["hits"] = s.Hits,
            ["misses"] = s.Misses,
            ["hitRatio"] = Math.Round(s.HitRatio, 3),
            ["invalidations"] = s.Invalidations,
            ["evictions"] = s.Evictions,
            ["entries"] = s.EntryCount,
            ["estimatedSize"] = s.EstimatedSize ?? 0
        };

        return Task.FromResult(HealthCheckResult.Healthy("In-memory cache is operational", data));
    }
}