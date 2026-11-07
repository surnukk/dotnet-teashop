using System.Diagnostics;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Catalog.Api.Grpc; 

/// <summary>Серверний interceptor: структуроване логування вхідних gRPC-викликів + атрибути поточного span.</summary>
public class LoggingInterceptor(ILogger<LoggingInterceptor> logger) : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var correlationId = context.RequestHeaders.GetValue("x-correlation-id");
        var activity = Activity.Current;
        activity?.SetTag("app.grpc.peer", context.Peer);
        activity?.SetTag("app.grpc.request_type", typeof(TRequest).Name);
        if (correlationId is not null)
            activity?.SetTag("app.correlation_id", correlationId);

        var sw = Stopwatch.StartNew();
        try
        {
            var response = await continuation(request, context);
            logger.LogInformation(
                "gRPC {Method} from {Peer}: {Status} in {Elapsed} ms (CorrelationId {CorrelationId})",
                context.Method, context.Peer, StatusCode.OK, sw.ElapsedMilliseconds, correlationId);
            return response;
        }
        catch (RpcException ex)
        {
            if (ex.StatusCode is StatusCode.NotFound or StatusCode.InvalidArgument
                or StatusCode.FailedPrecondition or StatusCode.AlreadyExists or StatusCode.Cancelled)
            {
                logger.LogWarning(
                    "gRPC {Method} from {Peer}: {Status} in {Elapsed} ms (CorrelationId {CorrelationId})",
                    context.Method, context.Peer, ex.StatusCode, sw.ElapsedMilliseconds, correlationId);
            }
            else
            {
                logger.LogError(ex,
                    "gRPC {Method} from {Peer}: {Status} in {Elapsed} ms (CorrelationId {CorrelationId})",
                    context.Method, context.Peer, ex.StatusCode, sw.ElapsedMilliseconds, correlationId);
            }
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "gRPC {Method} from {Peer}: Unhandled exception in {Elapsed} ms (CorrelationId {CorrelationId})",
                context.Method, context.Peer, sw.ElapsedMilliseconds, correlationId);
            throw;
        }
    }
}