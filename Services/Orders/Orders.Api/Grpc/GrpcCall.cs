using System.Diagnostics;
using Grpc.Core;
using Orders.Bll.Caching;
using Orders.Domain.Exceptions;
using Serilog.Context;

namespace Orders.Api.GrpcServices;

internal static class GrpcCall
{
    public static async Task<TResponse> RunAsync<TResponse>(
        string method,
        ServerCallContext context,
        ILogger logger,
        Func<CancellationToken, Task<TResponse>> action)
    {
        var cache = CacheCallContext.Begin();
        var stopwatch = Stopwatch.StartNew();

        // Ці властивості потрапляють у всі логи всередині виклику, зокрема в логи кешу
        using var protocolProperty = LogContext.PushProperty("Protocol", "gRPC");
        using var methodProperty = LogContext.PushProperty("GrpcMethod", method);

        try
        {
            var response = await action(context.CancellationToken);

            context.ResponseTrailers.Add("x-cache", cache.Result);   // ознака влучання для клієнта
            logger.LogInformation(
                "gRPC {GrpcMethod} succeeded in {ElapsedMs} ms, cache {CacheResult} (L1 hits {L1Hits}, L2 hits {L2Hits}, misses {CacheMisses})",
                method, stopwatch.ElapsedMilliseconds, cache.Result, cache.L1Hits, cache.L2Hits, cache.Misses);

            return response;
        }
        catch (Exception ex)
        {
            var rpc = ToRpcException(ex);

            if (rpc.StatusCode == StatusCode.Internal)
                logger.LogError(ex, "gRPC {GrpcMethod} failed with {StatusCode}", method, rpc.StatusCode);
            else
                logger.LogWarning("gRPC {GrpcMethod} rejected with {StatusCode}: {Detail}",
                    method, rpc.StatusCode, rpc.Status.Detail);

            throw rpc;
        }
    }

    private static RpcException ToRpcException(Exception ex) => ex switch
    {
        RpcException rpc => rpc,
        NotFoundException => new RpcException(new Status(StatusCode.NotFound, ex.Message)),
        ValidationException => new RpcException(new Status(StatusCode.InvalidArgument, ex.Message)),
        BusinessConflictException => new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message)),
        OperationCanceledException => new RpcException(new Status(StatusCode.Cancelled, "Request was cancelled")),
        _ => new RpcException(new Status(StatusCode.Internal, "Internal server error"))
    };
}