using Orders.Bll.Caching;
using Orders.Dal.Interfaces;
using Platform.EventBus.Events;

namespace Orders.Api.IntegrationEvents;

public sealed class ProductDeletedHandler(IUnitOfWork uow, ICacheService cache, ILogger<ProductDeletedHandler> logger)
    : ProductSyncHandler<ProductDeletedEvent>(uow, cache, logger)
{
    protected override long ProductIdOf(ProductDeletedEvent e) => e.ProductId;

    protected override async Task<bool> ApplyAsync(ProductDeletedEvent e, CancellationToken ct)
    {
        var deleted = await Uow.Products.DeleteSnapshotAsync(e.ProductId, ct);

        if (!deleted)
            Logger.LogInformation("Product {ProductId} kept in local copy: it is referenced by existing order items (or is absent)",
                e.ProductId);
        return deleted;
    }
}