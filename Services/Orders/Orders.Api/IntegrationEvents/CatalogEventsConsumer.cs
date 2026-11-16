using Platform.EventBus.Consuming;
using Platform.EventBus.Topology;
using RabbitMQ.Client;

namespace Orders.Api.IntegrationEvents;

public sealed class CatalogEventsConsumer(
    IConnection connection,
    IServiceScopeFactory scopes,
    EventHandlerRegistry registry,
    ILogger<CatalogEventsConsumer> logger)
    : RabbitMqConsumerBase(connection, scopes, registry, logger)
{
    protected override string QueueName => EventBusTopology.OrdersCatalogSyncQueue;   // "orders.catalog-sync"
}