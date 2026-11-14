using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Reviews.WebApi.HealthChecks;

public static class ReviewsHealthChecks
{
    public static WebApplicationBuilder AddReviewsHealthChecks(this WebApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            // 3.2: MongoDB: легкий ping (MongoHealthCheck), зрозуміла назва, теги, таймаут 5 с
            .AddCheck<MongoHealthCheck>(
                "reviews-db",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["database", "mongodb", "ready"],
                timeout: TimeSpan.FromSeconds(5))
            // 3.3: Redis: Degraded, бо кеш необов'язковий
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