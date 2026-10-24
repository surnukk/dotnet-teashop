using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Reviews.Domain.Entities;
using Reviews.Domain.Exceptions;
using Reviews.Domain.Interfaces;

namespace Reviews.Infrastructure.Persistence;

public class ReviewRepository(MongoDbContext context) : IReviewRepository
{
    private readonly IMongoCollection<Review> _reviews = context.Reviews;

    public async Task<Review?> GetByIdAsync(ObjectId id, CancellationToken ct = default)
        => await _reviews.Find(r => r.Id == id).FirstOrDefaultAsync(ct);

    public async Task<bool> ExistsAsync(long productId, string authorId, CancellationToken ct = default)
        => await _reviews
            .Find(r => r.ProductId == productId && r.Author.UserId == authorId)
            .Limit(1)
            .AnyAsync(ct);

    public Task AddAsync(Review review, CancellationToken ct = default)
        => _reviews.InsertOneAsync(review, cancellationToken: ct);

    public async Task UpdateAsync(Review review, CancellationToken ct = default)
    {
        var expectedVersion = review.Version;
        review.BumpVersion();

        // оновлюємо лише документ із очікуваною версією
        var filter = Builders<Review>.Filter.And(
            Builders<Review>.Filter.Eq(r => r.Id, review.Id),
            Builders<Review>.Filter.Eq(r => r.Version, expectedVersion));

        var u = Builders<Review>.Update;
        var update = u.Combine(
            u.Set(r => r.Rating, review.Rating),
            u.Set(r => r.Text, review.Text),
            u.Set("comments", review.Comments.ToList()),
            u.Set(r => r.UpdatedAt, review.UpdatedAt),
            u.Set(r => r.Version, review.Version));

        var result = await _reviews.UpdateOneAsync(filter, update, cancellationToken: ct);

        if (result.MatchedCount == 0)   // хтось змінив або видалив документ раніше
            throw new ConflictException($"Review {review.Id} was modified or deleted by someone else");
    }

    public async Task<bool> DeleteAsync(ObjectId id, CancellationToken ct = default)
    {
        var result = await _reviews.DeleteOneAsync(r => r.Id == id, ct);
        return result.DeletedCount > 0;
    }
}