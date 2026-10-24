using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Reviews.Domain.Entities;
using Reviews.Domain.ValueObjects;
using Reviews.Infrastructure.Persistence;

namespace Reviews.Infrastructure.Seeding;

public class ReviewSeeder(MongoDbContext context, ILogger<ReviewSeeder> logger) : IDataSeeder
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        var toInsert = new List<Review>();

        foreach (var review in BuildSeedReviews())
        {
            var exists = await context.Reviews
                .Find(r => r.ProductId == review.ProductId && r.Author.UserId == review.Author.UserId)
                .Limit(1)
                .AnyAsync(ct);

            if (!exists)
                toInsert.Add(review);
        }

        if (toInsert.Count == 0)
        {
            logger.LogInformation("Reviews are already seeded, nothing to insert");
            return;
        }

        await context.Reviews.InsertManyAsync(toInsert, cancellationToken: ct);
        logger.LogInformation("Seeded {Count} reviews", toInsert.Count);
    }

    private static List<Review> BuildSeedReviews()
    {
        var daHongPao = new Review(4, "Da Hong Pao Oolong", new Author("1", "Olena"), new Rating(5),
            "Deep mineral taste, holds up to six infusions.");
        daHongPao.AddComment(new Author("2", "Ivan"), "Agreed, a really good oolong.");

        var shuPuerh = new Review(3, "Shu Pu-erh Aged 2018", new Author("2", "Ivan"), new Rating(4),
            "Smooth and earthy, great for the evening.");

        var gaiwan = new Review(5, "Ceramic Gaiwan Set", new Author("3", "Maria"), new Rating(5),
            "Comfortable gaiwan, opens up the aroma nicely.");

        return [daHongPao, shuPuerh, gaiwan];
    }
}