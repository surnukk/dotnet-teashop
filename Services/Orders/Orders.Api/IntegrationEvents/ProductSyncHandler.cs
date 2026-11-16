using Orders.Bll.Caching;
using Orders.Dal.Interfaces;
using Platform.EventBus.Abstractions;
using Platform.EventBus.Events;

namespace Orders.Api.IntegrationEvents;

public abstract class ProductSyncHandler<TEvent>(IUnitOfWork uow, ICacheService cache, ILogger logger)
    : IIntegrationEventHandler<TEvent> where TEvent : IntegrationEvent
{
    protected IUnitOfWork Uow => uow;
    protected ILogger Logger => logger;

    protected abstract long ProductIdOf(TEvent e);

    /// <returns>true, якщо копію даних змінено</returns>
    protected abstract Task<bool> ApplyAsync(TEvent e, CancellationToken ct);

    public async Task HandleAsync(TEvent e, CancellationToken ct)
    {
        bool changed;

        await uow.BeginTransactionAsync(ct: ct);
        try
        {
            // EventId і бізнес-зміна — в одній транзакції
            if (!await uow.ProcessedEvents.TryAddAsync(e.EventId, ct))
            {
                logger.LogInformation("Duplicate event {EventId} ({EventType}) skipped", e.EventId, typeof(TEvent).Name);
                await uow.RollbackAsync(ct);
                return;
            }

            changed = await ApplyAsync(e, ct);
            await uow.CommitAsync(ct);
        }
        catch
        {
            await uow.RollbackAsync(ct);
            throw;
        }

        if (changed)
        {
            var id = ProductIdOf(e);
            await cache.RemoveAsync(OrdersCache.Product(id), typeof(TEvent).Name, ct);
            await cache.RemoveAsync(OrdersCache.Products, typeof(TEvent).Name, ct);
        }

        logger.LogInformation("Handled {EventType} {EventId} for product {ProductId} (copy changed: {Changed})",
            typeof(TEvent).Name, e.EventId, ProductIdOf(e), changed);
    }
}