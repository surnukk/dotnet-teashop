using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Reviews.Domain.Entities;
using Reviews.Infrastructure.Persistence;

namespace Reviews.Infrastructure.Seeding;

public class IndexCreationService(MongoDbContext context, ILogger<IndexCreationService> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        var keys = Builders<Review>.IndexKeys;

        try
        {
            await context.Reviews.Indexes.CreateManyAsync(new[]
            {
                // «останні відгуки про товар» (і підтримка сортування в ReviewQueries)
                new CreateIndexModel<Review>(
                    keys.Ascending(r => r.ProductId).Descending(r => r.CreatedAt)),

                // повнотекстовий пошук по тексту відгуку
                new CreateIndexModel<Review>(keys.Text(r => r.Text)),

                // один відгук від автора на товар
                new CreateIndexModel<Review>(
                    keys.Ascending(r => r.ProductId).Ascending("author.userId"),
                    new CreateIndexOptions { Unique = true })
            }, ct);

            logger.LogInformation("MongoDB indexes for 'reviews' are in place");
        }
        catch (Exception ex) when (ex is MongoException or TimeoutException)
        {
            logger.LogError(ex, "Failed to create MongoDB indexes for 'reviews'");
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}