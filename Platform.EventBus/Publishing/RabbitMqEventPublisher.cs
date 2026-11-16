using System.Text.Json;
using Microsoft.Extensions.Logging;
using Platform.EventBus.Abstractions;
using Platform.EventBus.Events;
using Platform.EventBus.Serialization;
using RabbitMQ.Client;
using Platform.EventBus.Tracing;

namespace Platform.EventBus.Publishing;

public sealed class RabbitMqEventPublisher(IConnection connection, ILogger<RabbitMqEventPublisher> logger)
    : IEventPublisher
{
    public async Task PublishAsync<T>(T @event, string exchange, string routingKey, CancellationToken ct = default)
        where T : IntegrationEvent
    {
        var eventType = @event.GetType().Name;

        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);

        var headers = new Dictionary<string, object?>
        {
            ["x-schema-version"] = @event.SchemaVersion
        };
        if (!string.IsNullOrEmpty(@event.CorrelationId))
            headers["x-correlation-id"] = @event.CorrelationId;

        var props = new BasicProperties
        {
            Persistent = true,                       // delivery_mode = 2
            ContentType = "application/json",
            MessageId = @event.EventId.ToString(),   // MessageId = EventId
            Type = eventType,                        // тип події
            Timestamp = new AmqpTimestamp(new DateTimeOffset(@event.OccurredAt.ToUniversalTime()).ToUnixTimeSeconds()),
            Headers = headers
        };

        using var activity = TracePropagation.StartProducerActivity(
    exchange, routingKey, eventType, props.MessageId, @event.CorrelationId);
        TracePropagation.Inject(props);       // traceparent (+ tracestate, baggage) у заголовки

        var body = JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType(), JsonDefaults.Options);

        await channel.BasicPublishAsync(exchange, routingKey, mandatory: false, props, body, ct);

        logger.LogInformation(
            "Published {EventType} {EventId} (CorrelationId {CorrelationId}) to {Exchange}/{RoutingKey}",
            eventType, @event.EventId, @event.CorrelationId, exchange, routingKey);
    }
}