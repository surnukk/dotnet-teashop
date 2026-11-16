using MongoDB.Driver;
using Platform.EventBus.Abstractions;
using Platform.EventBus.Events;
using Reviews.Application.Common.Caching;
using Reviews.Domain.Entities;
using Reviews.Infrastructure.Persistence;

namespace Reviews.WebApi.IntegrationEvents;

public sealed class ProductRenamedHandler(
    MongoDbContext db,
    ICacheService cache,
    ILogger<ProductRenamedHandler> logger) : IIntegrationEventHandler<ProductRenamedEvent>
{
    public async Task HandleAsync(ProductRenamedEvent e, CancellationToken ct)
    {
        long productId = e.ProductId;
        var eventId = e.EventId.ToString();

        // 1) ідемпотентність: подію вже оброблено
        if (await db.ProcessedEvents.Find(x => x.Id == eventId).AnyAsync(ct))
        {
            logger.LogInformation("Duplicate event {EventId} skipped", e.EventId);
            return;
        }

        // 2) out-of-order: не перезаписуємо новішу назву старішою подією
        var snapshot = await db.ProductNames.Find(x => x.Id == productId).FirstOrDefaultAsync(ct);
        if (snapshot is not null && snapshot.UpdatedAt >= e.OccurredAt)
        {
            logger.LogInformation(
                "Stale rename {EventId} for product {ProductId} ignored (newer name already applied)",
                e.EventId, productId);
        }
        else
        {
            // 3) оновлюємо денормалізовану назву в усіх відгуках про товар
            var result = await db.Reviews.UpdateManyAsync(
                r => r.ProductId == productId,
                Builders<Review>.Update.Set(r => r.ProductName, e.NewName),
                cancellationToken: ct);

            // 4) запам'ятовуємо час застосованої зміни
            await AdvanceSnapshotAsync(productId, e.NewName, e.OccurredAt, ct);

            await cache.RemoveGroupAsync(ReviewsCache.ListsGroup, "ProductRenamed", ct);

            logger.LogInformation(
                "Product {ProductId} renamed to {NewName}: {Modified} reviews updated (event {EventId})",
                productId, e.NewName, result.ModifiedCount, e.EventId);
        }

        // 5) позначаємо подію обробленою — останнім кроком
        await MarkProcessedAsync(eventId, ct);
    }

    private async Task AdvanceSnapshotAsync(long productId, string name, DateTime at, CancellationToken ct)
    {
        var filter = Builders<ProductNameSnapshot>.Filter.And(
            Builders<ProductNameSnapshot>.Filter.Eq(x => x.Id, productId),
            Builders<ProductNameSnapshot>.Filter.Lt(x => x.UpdatedAt, at));
        var update = Builders<ProductNameSnapshot>.Update
            .Set(x => x.ProductName, name)
            .Set(x => x.UpdatedAt, at);

        try
        {
            await db.ProductNames.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true }, ct);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            // документ уже має новішу мітку — нічого оновлювати
        }
    }

    private async Task MarkProcessedAsync(string eventId, CancellationToken ct)
    {
        try
        {
            await db.ProcessedEvents.InsertOneAsync(
                new ProcessedEventDocument { Id = eventId, ProcessedAt = DateTime.UtcNow },
                cancellationToken: ct);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            // паралельна доставка вже записала EventId
        }
    }
}