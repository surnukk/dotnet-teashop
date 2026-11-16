using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Platform.EventBus.Topology;

public sealed class RabbitMqTopologyInitializer(
    IConnection connection,
    ILogger<RabbitMqTopologyInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var channel = await connection.CreateChannelAsync(
            cancellationToken: cancellationToken);

        await EventBusTopology.DeclareAsync(channel, cancellationToken);

        logger.LogInformation(
            "RabbitMQ topology declared: exchanges {CatalogExchange}, {OrdersExchange}; " +
            "queues {OrdersQueue}, {ReviewsQueue} (with DLQ)",
            EventBusTopology.CatalogExchange, EventBusTopology.OrdersExchange,
            EventBusTopology.OrdersCatalogSyncQueue, EventBusTopology.ReviewsCatalogSyncQueue);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}