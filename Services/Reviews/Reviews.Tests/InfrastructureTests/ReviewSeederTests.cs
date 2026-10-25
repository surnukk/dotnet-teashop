using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;
using Reviews.Domain.Entities;
using Reviews.Infrastructure.Seeding;

namespace Reviews.Tests.InfrastructureTests;

[Trait("Category", "Integration")]
public class ReviewSeederTests : MongoTestBase
{
    private ReviewSeeder NewSeeder() => new(Context, NullLogger<ReviewSeeder>.Instance);

    private Task<long> CountAsync()
        => Context.Reviews.CountDocumentsAsync(FilterDefinition<Review>.Empty);

    [Fact]
    public async Task Seeding_empty_database_creates_three_reviews()
    {
        await NewSeeder().SeedAsync();

        Assert.Equal(3L, await CountAsync());
    }

    [Fact]
    public async Task Seeding_twice_does_not_duplicate_data()
    {
        await NewSeeder().SeedAsync();
        await NewSeeder().SeedAsync();

        Assert.Equal(3L, await CountAsync());
    }

    [Fact]
    public async Task Seeder_skips_reviews_that_already_exist()
    {
        await Repository.AddAsync(NewReview(4, "1"));   // той самий товар і автор, що в одному з сід-відгуків

        await NewSeeder().SeedAsync();

        Assert.Equal(3L, await CountAsync());           // 1 існуючий + 2 нових
    }

    [Fact]
    public async Task Seeding_works_together_with_unique_index()
    {
        await new IndexCreationService(Context, NullLogger<IndexCreationService>.Instance)
            .StartAsync(default);

        await NewSeeder().SeedAsync();
        await NewSeeder().SeedAsync();                  // повтор не порушує унікальний індекс

        Assert.Equal(3L, await CountAsync());
    }
}