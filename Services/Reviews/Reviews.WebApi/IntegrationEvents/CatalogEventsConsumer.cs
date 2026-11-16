using Platform.EventBus.Consuming;
using Platform.EventBus.Monitoring;
using Platform.EventBus.Topology;
using RabbitMQ.Client;

namespace Reviews.WebApi.IntegrationEvents;

public sealed class CatalogEventsConsumer(
    IConnection connection,
    IServiceScopeFactory scopes,
    EventHandlerRegistry registry,
    EventBusMetrics metrics,
    ILogger<CatalogEventsConsumer> logger)
    : RabbitMqConsumerBase(connection, scopes, registry, metrics, logger)
{
    protected override string QueueName => EventBusTopology.ReviewsCatalogSyncQueue;
}