using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace Platform.ServiceDefaults.HealthChecks;

/// <summary>Startup-перевірка: Healthy лише після того, як застосунок повністю запустився.</summary>
public sealed class StartupHealthCheck : IHealthCheck
{
    private volatile bool _started;

    public StartupHealthCheck(IHostApplicationLifetime lifetime) =>
        lifetime.ApplicationStarted.Register(() => _started = true);

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default) =>
        Task.FromResult(_started
            ? HealthCheckResult.Healthy("Application started")
            : HealthCheckResult.Unhealthy("Application is still starting"));
}