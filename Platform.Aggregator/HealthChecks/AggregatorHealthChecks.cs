using Grpc.Health.V1;
using Grpc.Net.ClientFactory;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Platform.Aggregator.HealthChecks;

public static class AggregatorHealthChecks
{
    public static WebApplicationBuilder AddAggregatorHealthChecks(this WebApplicationBuilder builder)
    {
        AddHealthClient(builder, "orders-health", "https://orders-api");
        AddHealthClient(builder, "catalog-health", "https://catalog-api");
        AddHealthClient(builder, "reviews-health", "https://reviews-api");

        var checks = builder.Services.AddHealthChecks();

        AddGrpcCheck(checks, "orders-api", "orders-health", HealthStatus.Unhealthy);
        AddGrpcCheck(checks, "catalog-api", "catalog-health", HealthStatus.Unhealthy);
        AddGrpcCheck(checks, "reviews-api", "reviews-health", HealthStatus.Degraded);

        checks.AddRedis(
            redisConnectionString: builder.Configuration.GetConnectionString("redis")!,
            name: "redis-cache",
            failureStatus: HealthStatus.Degraded,
            tags: ["cache", "redis", "ready"],
            timeout: TimeSpan.FromSeconds(3));

        return builder;
    }

    private static void AddHealthClient(WebApplicationBuilder builder, string name, string address) =>
        builder.Services.AddGrpcClient<Health.HealthClient>(name, o => o.Address = new Uri(address))
            .RemoveAllResilienceHandlers();

    private static void AddGrpcCheck(IHealthChecksBuilder checks, string name, string clientName, HealthStatus failureStatus) =>
        checks.Add(new HealthCheckRegistration(
            name,
            sp => new GrpcServiceHealthCheck(
                sp.GetRequiredService<GrpcClientFactory>(),
                sp.GetRequiredService<ILogger<GrpcServiceHealthCheck>>(),
                clientName,
                name),
            failureStatus,
            tags: ["grpc", "downstream", "ready"],
            timeout: TimeSpan.FromSeconds(4)));
}