using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Platform.EventBus.Events;
using Platform.EventBus.Serialization;
using Platform.EventBus.Topology;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Platform.EventBus.Consuming;

public abstract class RabbitMqConsumerBase(
    IConnection connection,
    IServiceScopeFactory scopes,
    EventHandlerRegistry registry,
    ILogger logger) : BackgroundService
{
    protected abstract string QueueName { get; }
    protected virtual ushort PrefetchCount => 10;

    protected IChannel? Channel { get; private set; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        Channel = channel;

        await EventBusTopology.DeclareAsync(channel, stoppingToken);   // повторне оголошення безпечне
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: PrefetchCount, global: false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += OnMessageAsync;

        var consumerTag = await channel.BasicConsumeAsync(QueueName, autoAck: false, consumer, stoppingToken);
        logger.LogInformation("Consuming from queue {Queue} (prefetch {Prefetch})", QueueName, PrefetchCount);

        try { await Task.Delay(Timeout.Infinite, stoppingToken); }
        catch (OperationCanceledException) { /* застосунок зупиняється */ }

        await channel.BasicCancelAsync(consumerTag, noWait: false, CancellationToken.None);
        logger.LogInformation("Consumer for queue {Queue} stopped", QueueName);
    }

    private async Task OnMessageAsync(object sender, BasicDeliverEventArgs ea)
    {
        var messageId = ea.BasicProperties.MessageId;
        var eventType = ea.BasicProperties.Type;
        var correlationId = ReadHeader(ea, "x-correlation-id");

        using var logScope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["EventId"] = messageId,
            ["EventType"] = eventType,
            ["CorrelationId"] = correlationId,
            ["Queue"] = QueueName
        });

        try
        {
            if (!registry.TryGet(eventType, out var registration))
            {
                logger.LogDebug("No handler for {EventType} in queue {Queue}; message ignored", eventType, QueueName);
                await AckAsync(ea);
                return;
            }

            var @event = Deserialize(ea, registration.EventType);
            logger.LogInformation("Received {EventType} {EventId}", eventType, @event.EventId);

            await using var scope = scopes.CreateAsyncScope();     // залежності — у межах scope повідомлення
            await registration.Handle(scope.ServiceProvider, @event, CancellationToken.None);
        }
        catch (JsonException ex)                                   // постійний збій: повтор не допоможе
        {
            logger.LogError(ex, "Malformed message {MessageId}, sending to DLQ", messageId);
            await NackToDeadLetterAsync(ea, "malformed message");
            return;
        }
        catch (Exception ex)                                       // збій обробки
        {
            await RetryOrDeadLetterAsync(ea, ex);
            return;
        }

        await AckAsync(ea);                                        // успіх
    }

    private static IntegrationEvent Deserialize(BasicDeliverEventArgs ea, Type eventType)
        => JsonSerializer.Deserialize(ea.Body.Span, eventType, JsonDefaults.Options) as IntegrationEvent
           ?? throw new JsonException("Empty message");

    protected async Task AckAsync(BasicDeliverEventArgs ea)
    {
        await Channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
        logger.LogInformation("Message {MessageId} acknowledged", ea.BasicProperties.MessageId);
    }

    protected async Task NackToDeadLetterAsync(BasicDeliverEventArgs ea, string reason)
    {
        await Channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);   // → DLX → DLQ
        logger.LogWarning("Message {MessageId} rejected to DLQ: {Reason}", ea.BasicProperties.MessageId, reason);
    }

    protected virtual async Task RetryOrDeadLetterAsync(BasicDeliverEventArgs ea, Exception ex)
    {
        logger.LogError(ex, "Failed to process {EventType} {MessageId}",
            ea.BasicProperties.Type, ea.BasicProperties.MessageId);
        await NackToDeadLetterAsync(ea, "processing failed");
    }

    protected static string? ReadHeader(BasicDeliverEventArgs ea, string key)
        => ea.BasicProperties.Headers is { } headers && headers.TryGetValue(key, out var value)
            ? value switch
            {
                byte[] bytes => Encoding.UTF8.GetString(bytes),
                string s => s,
                null => null,
                _ => value.ToString()
            }
            : null;
}