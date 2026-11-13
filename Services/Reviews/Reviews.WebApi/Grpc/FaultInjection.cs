using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Reviews.WebApi.Grpc;  

/// <summary>Тестовий прапорець штучної затримки (лише Development, для перевірки таймаутів resilience).</summary>
public sealed class FaultInjection
{
    public volatile int DelayMs;
}

public class FaultInjectionInterceptor(FaultInjection faults) : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var delay = faults.DelayMs;
        if (delay > 0)
            await Task.Delay(delay, context.CancellationToken);
        return await continuation(request, context);
    }
}