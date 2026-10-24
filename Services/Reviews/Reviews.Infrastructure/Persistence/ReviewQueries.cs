using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Reviews.Application.Common.Interfaces;
using Reviews.Application.Common.Models;
using Reviews.Domain.Entities;

namespace Reviews.Infrastructure.Persistence;

public class ReviewQueries(MongoDbContext context) : IReviewQueries
{
    private readonly IMongoCollection<Review> _reviews = context.Reviews;

    private static readonly ProjectionDefinition<Review, BsonDocument> Projection =
        new BsonDocumentProjectionDefinition<Review, BsonDocument>(new BsonDocument
        {
            { "productId", 1 },
            { "productName", 1 },
            { "rating", 1 },
            { "text", 1 },
            { "version", 1 },
            { "author.displayName", 1 },
            { "commentsCount", new BsonDocument("$size",
                new BsonDocument("$ifNull", new BsonArray { "$comments", new BsonArray() })) }
        });

    public async Task<ReviewDto?> GetByIdAsync(string id, CancellationToken ct)
    {
        if (!ObjectId.TryParse(id, out var objectId))
            return null;

        var doc = await _reviews.Aggregate()
            .Match(r => r.Id == objectId)
            .Project(Projection)
            .FirstOrDefaultAsync(ct);

        return doc is null ? null : ToDto(doc);
    }

    public async Task<PagedResult<ReviewDto>> GetListAsync(
        long? productId, int page, int pageSize, CancellationToken ct)
    {
        var filter = productId.HasValue
            ? Builders<Review>.Filter.Eq(r => r.ProductId, productId.Value)
            : FilterDefinition<Review>.Empty;

        var total = await _reviews.CountDocumentsAsync(filter, cancellationToken: ct);

        var docs = await _reviews.Aggregate()
            .Match(filter)
            .Sort(Builders<Review>.Sort.Descending(r => r.CreatedAt).Descending(r => r.Id))
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .Project(Projection)
            .ToListAsync(ct);

        return new PagedResult<ReviewDto>(docs.Select(ToDto).ToList(), page, pageSize, total);
    }

    private static ReviewDto ToDto(BsonDocument d) => new(
        d["_id"].AsObjectId.ToString(),
        d["productId"].ToInt64(),
        d["productName"].AsString,
        d["author"]["displayName"].AsString,
        d["rating"].ToInt32(),
        d["text"].AsString,
        d["commentsCount"].ToInt32(),
        d.GetValue("version", 0).ToInt32());
}