using Grpc.Core;
using Grpc.Health.V1;
using Grpc.Net.ClientFactory;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Platform.Aggregator.HealthChecks;

/// <summary>Перевірка downstream-сервісу через стандартний gRPC Health Checking Protocol.</summary>
public sealed class GrpcServiceHealthCheck(
    GrpcClientFactory clients,
    ILogger<GrpcServiceHealthCheck> logger,
    string clientName,
    string serviceName) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        var client = clients.CreateClient<Health.HealthClient>(clientName);
        try
        {
            var reply = await client.CheckAsync(new HealthCheckRequest(),
                     deadline: DateTime.UtcNow.AddSeconds(3), cancellationToken: ct);

            return reply.Status == HealthCheckResponse.Types.ServingStatus.Serving
                ? HealthCheckResult.Healthy($"{serviceName} is serving")
                : new HealthCheckResult(context.Registration.FailureStatus, $"{serviceName} status: {reply.Status}");
        }
        catch (RpcException ex)
        {
            logger.LogWarning(ex, "{Service} gRPC health check failed with {StatusCode}", serviceName, ex.StatusCode);
            return new HealthCheckResult(context.Registration.FailureStatus, $"{serviceName} is unavailable", ex);
        }
    }
}