using System.Diagnostics;
using System.Text;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Platform.EventBus.Tracing;

public static class TracePropagation
{
    public const string SourceName = "Platform.Messaging";
    public static readonly ActivitySource Source = new(SourceName);

    private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

    public static Activity? StartProducerActivity(
        string exchange, string routingKey, string eventType, string? messageId, string? correlationId)
    {
        var activity = Source.StartActivity($"{exchange} publish", ActivityKind.Producer);
        if (activity is null) return null;

        activity.SetTag("messaging.system", "rabbitmq");
        activity.SetTag("messaging.operation", "publish");
        activity.SetTag("messaging.destination.name", exchange);
        activity.SetTag("messaging.rabbitmq.destination.routing_key", routingKey);
        activity.SetTag("messaging.message.id", messageId);
        activity.SetTag("event.type", eventType);
        activity.SetTag("correlation.id", correlationId);
        return activity;
    }

    public static void Inject(BasicProperties props)
    {
        if (Activity.Current is not { } activity) return;

        props.Headers ??= new Dictionary<string, object?>();
        Propagator.Inject(
            new PropagationContext(activity.Context, Baggage.Current),
            props.Headers,
            (headers, key, value) => headers[key] = value);
    }

    public static Activity? StartConsumerActivity(BasicDeliverEventArgs ea, string queue)
    {
        var headers = ea.BasicProperties.Headers;
        var parent = Propagator.Extract(default, headers, GetHeaderValues);

        var activity = Source.StartActivity($"{queue} process", ActivityKind.Consumer, parent.ActivityContext);
        if (activity is null) return null;

        activity.SetTag("messaging.system", "rabbitmq");
        activity.SetTag("messaging.operation", "process");
        activity.SetTag("messaging.destination.name", queue);
        activity.SetTag("messaging.message.id", ea.BasicProperties.MessageId);
        activity.SetTag("event.type", ea.BasicProperties.Type);
        activity.SetTag("correlation.id", GetHeaderValues(headers, "x-correlation-id").FirstOrDefault());
        return activity;
    }

    private static IEnumerable<string> GetHeaderValues(IDictionary<string, object?>? headers, string key)
    {
        if (headers is not null && headers.TryGetValue(key, out var value))
        {
            switch (value)
            {
                case byte[] bytes: return new[] { Encoding.UTF8.GetString(bytes) };
                case string s: return new[] { s };
            }
        }
        return Array.Empty<string>();
    }
}