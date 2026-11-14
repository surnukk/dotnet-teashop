using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Catalog.Api.HealthChecks;

public static class CatalogHealthChecks
{
    public static WebApplicationBuilder AddCatalogHealthChecks(this WebApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("CatalogDb")
            ?? throw new InvalidOperationException("Connection string 'CatalogDb' is missing");

        builder.Services.AddHealthChecks()
            .AddSqlServer(
                connectionString: connectionString,
                name: "catalog-db",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["database", "sql", "ready"],
                timeout: TimeSpan.FromSeconds(5))
            .AddRedis(
                redisConnectionString: builder.Configuration.GetConnectionString("redis")!
                    + ",connectTimeout=2000,syncTimeout=2000,asyncTimeout=2000", 
                name: "redis-cache",
                failureStatus: HealthStatus.Degraded,
                tags: ["cache", "redis", "ready"],
                timeout: TimeSpan.FromSeconds(3));

        builder.Services.AddGrpcHealthChecks(o =>
            o.Services.Map("", context => context.Tags.Contains("database")));

        return builder;
    }
}