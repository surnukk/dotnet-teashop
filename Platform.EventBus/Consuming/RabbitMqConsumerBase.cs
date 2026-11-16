using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Platform.EventBus.Events;
using Platform.EventBus.Monitoring;
using Platform.EventBus.Serialization;
using Platform.EventBus.Topology;
using Platform.EventBus.Tracing;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Platform.EventBus.Consuming;

public abstract class RabbitMqConsumerBase(
    IConnection connection,
    IServiceScopeFactory scopes,
    EventHandlerRegistry registry,
    EventBusMetrics metrics,
    ILogger logger) : BackgroundService
{
    public const string RetryCountHeader = "x-retry-count";

    private CancellationToken _stoppingToken;

    protected abstract string QueueName { get; }
    protected virtual ushort PrefetchCount => 10;

    protected virtual int MaxRetries => 3;

    protected virtual TimeSpan RetryDelay(int attempt) => TimeSpan.FromSeconds(2 * attempt);

    protected IChannel? Channel { get; private set; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _stoppingToken = stoppingToken;

        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        Channel = channel;

        await EventBusTopology.DeclareAsync(channel, stoppingToken);
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: PrefetchCount, global: false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += OnMessageAsync;

        var consumerTag = await channel.BasicConsumeAsync(QueueName, autoAck: false, consumer, stoppingToken);
        logger.LogInformation("Consuming from queue {Queue} (prefetch {Prefetch}, max retries {MaxRetries})",
            QueueName, PrefetchCount, MaxRetries);

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

        using var activity = TracePropagation.StartConsumerActivity(ea, QueueName);

        using var logScope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["EventId"] = messageId,
            ["EventType"] = eventType,
            ["CorrelationId"] = correlationId,
            ["Queue"] = QueueName
        });

        IDisposable? entityScope = null;    
        try
        {
            try
            {
                if (!registry.TryGet(eventType, out var registration))
                {
                    logger.LogDebug("No handler for {EventType} in queue {Queue}; message ignored", eventType, QueueName);
                    await AckAsync(ea, "ignored");
                    return;
                }

                var @event = Deserialize(ea, registration.EventType);
                entityScope = logger.BeginScope(@event.GetLogContext());
                logger.LogInformation("Received {EventType} {EventId}", eventType, @event.EventId);

                await using var scope = scopes.CreateAsyncScope();
                await registration.Handle(scope.ServiceProvider, @event, CancellationToken.None);
            }
            catch (Exception ex) when (ex is JsonException or PermanentFailureException)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                logger.LogError(ex, "Permanent failure for message {MessageId} ({EventType}), sending to DLQ",
                    messageId, eventType);
                await NackToDeadLetterAsync(ea, $"permanent failure: {ex.GetType().Name}");
                return;
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                await RetryOrDeadLetterAsync(ea, ex);
                return;
            }

            await AckAsync(ea, "acked");
        }
        finally
        {
            entityScope?.Dispose();
        }
    }

    private static IntegrationEvent Deserialize(BasicDeliverEventArgs ea, Type eventType)
        => JsonSerializer.Deserialize(ea.Body.Span, eventType, JsonDefaults.Options) as IntegrationEvent
           ?? throw new JsonException("Empty message");

    protected async Task AckAsync(BasicDeliverEventArgs ea, string outcome)
    {
        await Channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
        metrics.Outcome(QueueName, outcome);
        logger.LogInformation("Message {MessageId} acknowledged ({Outcome})", ea.BasicProperties.MessageId, outcome);
    }

    protected async Task NackToDeadLetterAsync(BasicDeliverEventArgs ea, string reason)
    {
        await Channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
        metrics.Outcome(QueueName, "dead_lettered");
        logger.LogWarning("Message {MessageId} rejected to DLQ: {Reason}", ea.BasicProperties.MessageId, reason);
    }

    protected virtual async Task RetryOrDeadLetterAsync(BasicDeliverEventArgs ea, Exception ex)
    {
        var headers = ea.BasicProperties.Headers;
        var attempt = ReadRetryCount(headers);

        if (attempt >= MaxRetries)
        {
            logger.LogError(ex, "Message {MessageId} ({EventType}) failed after {Attempts} attempts, sending to DLQ",
                ea.BasicProperties.MessageId, ea.BasicProperties.Type, attempt);
            await NackToDeadLetterAsync(ea, $"retries exhausted after {attempt} attempts");
            return;
        }

        var next = attempt + 1;
        var delay = RetryDelay(next);
        logger.LogWarning(ex, "Message {MessageId} ({EventType}) failed, retry {Attempt}/{Max} in {Delay}",
            ea.BasicProperties.MessageId, ea.BasicProperties.Type, next, MaxRetries, delay);

        try { await Task.Delay(delay, _stoppingToken); }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Shutdown during retry back-off; message {MessageId} will be redelivered",
                ea.BasicProperties.MessageId);
            return;
        }

        var props = new BasicProperties(ea.BasicProperties)
        {
            Headers = new Dictionary<string, object?>(headers ?? new Dictionary<string, object?>())
            {
                [RetryCountHeader] = next
            }
        };

        await Channel!.BasicPublishAsync(string.Empty, QueueName, mandatory: false, props, ea.Body);
        await Channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
        metrics.Outcome(QueueName, "retried");
    }

    private static int ReadRetryCount(IDictionary<string, object?>? headers)
        => headers is not null && headers.TryGetValue(RetryCountHeader, out var value)
            ? value switch
            {
                int i => i,
                long l => (int)l,
                byte[] b when int.TryParse(Encoding.UTF8.GetString(b), out var n) => n,
                _ => 0
            }
            : 0;

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