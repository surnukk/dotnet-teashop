using Orders.Bll.Caching;
using Orders.Dal.Interfaces;
using Platform.EventBus.Events;

namespace Orders.Api.IntegrationEvents;

public sealed class ProductCreatedHandler(IUnitOfWork uow, ICacheService cache, ILogger<ProductCreatedHandler> logger)
    : ProductSyncHandler<ProductCreatedEvent>(uow, cache, logger)
{
    protected override long ProductIdOf(ProductCreatedEvent e) => e.ProductId;

    protected override Task<bool> ApplyAsync(ProductCreatedEvent e, CancellationToken ct)
        => Uow.Products.CreateSnapshotAsync(e.ProductId, e.ProductName, e.Price, e.OccurredAt, ct);
}