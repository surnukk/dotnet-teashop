using Orders.Bll.Caching;
using Orders.Dal.Interfaces;
using Platform.EventBus.Consuming;
using Platform.EventBus.Events;

namespace Orders.Api.IntegrationEvents;

public sealed class ProductRenamedHandler(IUnitOfWork uow, ICacheService cache, ILogger<ProductRenamedHandler> logger)
    : ProductSyncHandler<ProductRenamedEvent>(uow, cache, logger)
{
    protected override long ProductIdOf(ProductRenamedEvent e) => e.ProductId;

    protected override async Task<bool> ApplyAsync(ProductRenamedEvent e, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(e.NewName))
            throw new PermanentFailureException($"Empty name for product {e.ProductId}");

        var applied = await Uow.Products.UpdateNameSnapshotAsync(e.ProductId, e.NewName, e.OccurredAt, ct);

        if (!applied)
            Logger.LogInformation("Rename event {EventId} for product {ProductId} skipped (stale or product is not in local copy)",
                e.EventId, e.ProductId);
        return applied;
    }
}