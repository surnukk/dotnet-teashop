using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Reviews.Domain.Entities;
using Reviews.Domain.Exceptions;
using Reviews.Domain.ValueObjects;
using Reviews.Infrastructure.Persistence;
using Reviews.Infrastructure.Seeding;

namespace Reviews.Tests.InfrastructureTests;

/// <summary>Кожен тест працює у власній тимчасовій БД, яка видаляється після тесту.</summary>
public abstract class MongoTestBase : IDisposable
{
    private readonly string _dbName = "EastLeaf_ReviewsDB_Test_" + Guid.NewGuid().ToString("N");

    protected MongoDbContext Context { get; }
    protected ReviewRepository Repository { get; }
    protected ReviewQueries Queries { get; }

    protected MongoTestBase()
    {
        MongoMappings.Register();
        Context = new MongoDbContext(Options.Create(new MongoDbOptions
        {
            ConnectionString = "mongodb://localhost:27017/?serverSelectionTimeoutMS=3000",
            DatabaseName = _dbName
        }));
        Repository = new ReviewRepository(Context);
        Queries = new ReviewQueries(Context);
    }

    protected static Review NewReview(long productId = 4, string userId = "1", string name = "Olena")
        => new(productId, "Da Hong Pao Oolong", new Author(userId, name), new Rating(5), "Deep mineral taste");

    protected IMongoCollection<BsonDocument> RawReviews
        => Context.Database.GetCollection<BsonDocument>("reviews");

    /// <summary>Документ у форматі seed.js: з полями, яких немає в домені.</summary>
    protected async Task<ObjectId> InsertSeedLikeDocumentAsync()
    {
        var id = ObjectId.GenerateNewId();
        await RawReviews.InsertOneAsync(new BsonDocument
        {
            { "_id", id },
            { "productId", new BsonInt64(4) },
            { "productName", "Da Hong Pao Oolong" },
            { "author", new BsonDocument { { "userId", "1" }, { "displayName", "Olena" } } },
            { "rating", 5 },
            { "text", "Deep mineral taste, holds up to six infusions." },
            { "comments", new BsonArray
                {
                    new BsonDocument
                    {
                        { "_id", ObjectId.GenerateNewId() },
                        { "author", new BsonDocument { { "userId", "2" }, { "displayName", "Ivan" } } },
                        { "text", "Agreed" },
                        { "createdAt", new BsonDateTime(DateTime.UtcNow) }
                    }
                } },
            { "discussionId", ObjectId.GenerateNewId() },
            { "teaware", new BsonDocument { { "material", "Ceramic" }, { "capacityMl", 150 } } },
            { "version", 0 },
            { "createdAt", new BsonDateTime(DateTime.UtcNow) }
        });
        return id;
    }

    public void Dispose() => Context.Client.DropDatabase(_dbName);
}

[Trait("Category", "Integration")]
public class MongoRepositoryTests : MongoTestBase
{
    [Fact]
    public async Task Review_roundtrips_with_value_objects_and_comments()
    {
        var review = NewReview();
        var comment = review.AddComment(new Author("2", "Ivan"), "Agreed");
        await Repository.AddAsync(review);

        var loaded = await Repository.GetByIdAsync(review.Id);

        Assert.NotNull(loaded);
        Assert.Equal(new Author("1", "Olena"), loaded!.Author);
        Assert.Equal(new Rating(5), loaded.Rating);
        var loadedComment = Assert.Single(loaded.Comments);
        Assert.Equal(comment.Id, loadedComment.Id);
        Assert.Equal(new Author("2", "Ivan"), loadedComment.Author);
        Assert.Equal("Agreed", loadedComment.Text);
    }

    [Fact]
    public async Task Stored_document_is_camelCase_without_internal_dotnet_fields()
    {
        var review = NewReview();
        review.AddComment(new Author("2", "Ivan"), "Agreed");
        await Repository.AddAsync(review);

        var raw = await RawReviews.Find(FilterDefinition<BsonDocument>.Empty).FirstAsync();

        Assert.False(raw.Contains("_comments")); 
        Assert.True(raw["comments"].IsBsonArray); 
        Assert.True(raw["comments"][0]["_id"].IsObjectId);
        Assert.Equal("1", raw["author"]["userId"].AsString);
        Assert.Equal(BsonType.Int32, raw["rating"].BsonType);
        Assert.Equal(BsonType.Int64, raw["productId"].BsonType);
        Assert.Equal(0, raw["version"].AsInt32);
        Assert.All(raw.Names, n => Assert.True(n == "_id" || char.IsLower(n[0])));
    }

    [Fact]
    public async Task Update_bumps_version_and_keeps_fields_unknown_to_the_domain()
    {
        var id = await InsertSeedLikeDocumentAsync();
        var review = await Repository.GetByIdAsync(id);
        Assert.NotNull(review);
        Assert.Single(review!.Comments);

        review.Update(new Rating(4), "Changed my mind");
        await Repository.UpdateAsync(review);

        var raw = await RawReviews.Find(Builders<BsonDocument>.Filter.Eq("_id", id)).FirstAsync();
        Assert.Equal(1, raw["version"].AsInt32);
        Assert.Equal(4, raw["rating"].AsInt32);
        Assert.Equal("Changed my mind", raw["text"].AsString);
        Assert.True(raw.Contains("teaware"));
        Assert.True(raw.Contains("discussionId"));
        Assert.Single(raw["comments"].AsBsonArray);
    }

    [Fact]
    public async Task Update_with_stale_version_throws_conflict()
    {
        var review = NewReview();
        await Repository.AddAsync(review);

        var first = await Repository.GetByIdAsync(review.Id);
        var second = await Repository.GetByIdAsync(review.Id);

        first!.Update(new Rating(3), "First editor");
        await Repository.UpdateAsync(first);

        second!.Update(new Rating(2), "Second editor");
        await Assert.ThrowsAsync<ConflictException>(() => Repository.UpdateAsync(second));
    }

    [Fact]
    public async Task Index_service_creates_indexes_and_unique_index_rejects_duplicate_review()
    {
        await new IndexCreationService(Context, NullLogger<IndexCreationService>.Instance)
            .StartAsync(default);

        var indexes = await (await Context.Reviews.Indexes.ListAsync()).ToListAsync();
        Assert.Equal(4, indexes.Count);

        await Repository.AddAsync(NewReview(4, "1"));
        var ex = await Assert.ThrowsAsync<MongoWriteException>(
            () => Repository.AddAsync(NewReview(4, "1")));

        Assert.Equal(ServerErrorCategory.DuplicateKey, ex.WriteError.Category);
    }

    [Fact]
    public async Task ExistsAsync_finds_review_by_product_and_author()
    {
        await Repository.AddAsync(NewReview(4, "1"));

        Assert.True(await Repository.ExistsAsync(4, "1"));
        Assert.False(await Repository.ExistsAsync(4, "2"));
        Assert.False(await Repository.ExistsAsync(5, "1"));
    }

    [Fact]
    public async Task DeleteAsync_returns_true_once_then_false()
    {
        var review = NewReview();
        await Repository.AddAsync(review);

        Assert.True(await Repository.DeleteAsync(review.Id));
        Assert.False(await Repository.DeleteAsync(review.Id));
    }

    [Fact]
    public async Task Queries_list_filters_and_paginates_in_the_database()
    {
        await Repository.AddAsync(NewReview(4, "1"));
        await Repository.AddAsync(NewReview(4, "2"));
        await Repository.AddAsync(NewReview(4, "3"));
        await Repository.AddAsync(NewReview(3, "1"));

        var firstPage = await Queries.GetListAsync(4, 1, 2, default);
        var secondPage = await Queries.GetListAsync(4, 2, 2, default);
        var all = await Queries.GetListAsync(null, 1, 20, default);

        Assert.Equal(3, firstPage.TotalCount);
        Assert.Equal(2, firstPage.TotalPages);
        Assert.Equal(2, firstPage.Items.Count);
        Assert.Single(secondPage.Items);
        Assert.Equal(4, all.TotalCount);

        Assert.True(string.CompareOrdinal(firstPage.Items[0].Id, firstPage.Items[1].Id) > 0);
    }

    [Fact]
    public async Task Queries_getById_projects_dto_with_comments_count_and_version()
    {
        var id = await InsertSeedLikeDocumentAsync();

        var dto = await Queries.GetByIdAsync(id.ToString(), default);

        Assert.NotNull(dto);
        Assert.Equal(4, dto!.ProductId);
        Assert.Equal("Olena", dto.AuthorName);
        Assert.Equal(5, dto.Rating);
        Assert.Equal(1, dto.CommentsCount);
        Assert.Equal(0, dto.Version);
        Assert.Null(await Queries.GetByIdAsync(ObjectId.GenerateNewId().ToString(), default));
        Assert.Null(await Queries.GetByIdAsync("not-an-id", default));
    }
}