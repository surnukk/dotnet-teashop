using Orders.Bll.Caching;
using Orders.Dal.Interfaces;
using Platform.EventBus.Events;
using Platform.EventBus.Consuming;

namespace Orders.Api.IntegrationEvents;

public sealed class ProductCreatedHandler(IUnitOfWork uow, ICacheService cache, ILogger<ProductCreatedHandler> logger)
    : ProductSyncHandler<ProductCreatedEvent>(uow, cache, logger)
{
    protected override long ProductIdOf(ProductCreatedEvent e) => e.ProductId;

    protected override Task<bool> ApplyAsync(ProductCreatedEvent e, CancellationToken ct)
    {
        if (e.Price <= 0)
            throw new PermanentFailureException($"Invalid price {e.Price} for product {e.ProductId}");

        return Uow.Products.CreateSnapshotAsync(e.ProductId, e.ProductName, e.Price, e.OccurredAt, ct);
    }
}