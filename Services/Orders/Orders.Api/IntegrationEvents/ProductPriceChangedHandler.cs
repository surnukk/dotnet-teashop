using Orders.Bll.Caching;
using Orders.Dal.Interfaces;
using Platform.EventBus.Events;
using Platform.EventBus.Consuming;

namespace Orders.Api.IntegrationEvents;

public sealed class ProductPriceChangedHandler(IUnitOfWork uow, ICacheService cache, ILogger<ProductPriceChangedHandler> logger)
    : ProductSyncHandler<ProductPriceChangedEvent>(uow, cache, logger)
{
    protected override long ProductIdOf(ProductPriceChangedEvent e) => e.ProductId;

    protected override async Task<bool> ApplyAsync(ProductPriceChangedEvent e, CancellationToken ct)
    {
        if (e.NewPrice <= 0)
            throw new PermanentFailureException($"Invalid price {e.NewPrice} for product {e.ProductId}");

        var applied = await Uow.Products.UpdatePriceSnapshotAsync(
            e.ProductId, e.ProductName, e.NewPrice, e.OccurredAt, ct);

        if (!applied)
            Logger.LogInformation("Stale price event {EventId} for product {ProductId} ignored (newer price already applied)",
                e.EventId, e.ProductId);
        return applied;
    }
}