using RabbitMQ.Client;

namespace Platform.EventBus.Topology;

public static class EventBusTopology
{
    // Exchanges
    public const string CatalogExchange = "catalog-events";
    public const string CatalogDlx = "catalog-events.dlx";
    public const string OrdersExchange = "orders-events";
    public const string OrdersDlx = "orders-events.dlx";

    // Черги-споживачі (по одній на сервіс)
    public const string OrdersCatalogSyncQueue = "orders.catalog-sync";
    public const string ReviewsCatalogSyncQueue = "reviews.catalog-sync";

    // Routing keys подій Каталогу
    public const string ProductPriceChangedKey = "product.price_changed";
    public const string ProductRenamedKey = "product.renamed";
    public const string ProductBindingKey = "product.*";

    public static string DlqName(string queue) => $"{queue}.dlq";

    public static async Task DeclareAsync(IChannel channel, CancellationToken ct)
    {
        // Основні exchanges (topic) і exchanges для "мертвих" повідомлень (direct)
        await channel.ExchangeDeclareAsync(CatalogExchange, ExchangeType.Topic,
            durable: true, autoDelete: false, cancellationToken: ct);
        await channel.ExchangeDeclareAsync(CatalogDlx, ExchangeType.Direct,
            durable: true, autoDelete: false, cancellationToken: ct);

        await channel.ExchangeDeclareAsync(OrdersExchange, ExchangeType.Topic,
            durable: true, autoDelete: false, cancellationToken: ct);
        await channel.ExchangeDeclareAsync(OrdersDlx, ExchangeType.Direct,
            durable: true, autoDelete: false, cancellationToken: ct);

        // Черги синхронізації копій даних з подій Каталогу
        await DeclareQueueWithDlqAsync(channel, OrdersCatalogSyncQueue,
            CatalogExchange, CatalogDlx, ProductBindingKey, ct);
        await DeclareQueueWithDlqAsync(channel, ReviewsCatalogSyncQueue,
            CatalogExchange, CatalogDlx, ProductBindingKey, ct);
    }

    private static async Task DeclareQueueWithDlqAsync(
        IChannel channel, string queue, string exchange, string dlx,
        string bindingKey, CancellationToken ct)
    {
        var dlq = DlqName(queue);

        // 1) DLQ і її binding до DLX (routing key = назва основної черги)
        await channel.QueueDeclareAsync(dlq, durable: true, exclusive: false,
            autoDelete: false, cancellationToken: ct);
        await channel.QueueBindAsync(dlq, dlx, routingKey: queue, cancellationToken: ct);

        // 2) Основна черга: відхилені повідомлення йдуть у DLX зі своїм routing key
        var args = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = dlx,
            ["x-dead-letter-routing-key"] = queue
        };
        await channel.QueueDeclareAsync(queue, durable: true, exclusive: false,
            autoDelete: false, arguments: args, cancellationToken: ct);
        await channel.QueueBindAsync(queue, exchange, routingKey: bindingKey,
            cancellationToken: ct);
    }
}