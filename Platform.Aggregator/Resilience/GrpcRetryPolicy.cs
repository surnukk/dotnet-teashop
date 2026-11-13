using Grpc.Core;
using Grpc.Net.Client.Configuration;

namespace Platform.Aggregator.Resilience;

/// <summary>
/// 1.2: retry-політика gRPC-клієнта за gRPC-статусами (повторює лише Unavailable та ResourceExhausted).
/// Усі наші виклики читають дані, тому повторювати їх безпечно.
/// </summary>
public static class GrpcRetryPolicy
{
    public static ServiceConfig Create(int maxAttempts = 3) => new()
    {
        MethodConfigs =
        {
            new MethodConfig
            {
                Names = { MethodName.Default },                  
                RetryPolicy = new RetryPolicy
                {
                    MaxAttempts = maxAttempts,           
                    InitialBackoff = TimeSpan.FromSeconds(0.5),
                    MaxBackoff = TimeSpan.FromSeconds(4),
                    BackoffMultiplier = 2,                           
                    RetryableStatusCodes = { StatusCode.Unavailable, StatusCode.ResourceExhausted }
                }
            }
        }
    };
}