using MediatR;
using Reviews.Application.Commands.AddComment;
using Reviews.Application.Commands.CreateReview;
using Reviews.Application.Commands.DeleteReview;
using Reviews.Application.Commands.RemoveComment;
using Reviews.Application.Commands.UpdateReview;
using Reviews.Application.Common.Caching;

namespace Reviews.Application.Common.Behaviors;

/// <summary>
/// Інвалідація кешу після команд. Спрацьовує лише ПІСЛЯ успішного виконання команди
/// (якщо handler кинув виняток — до рядків нижче виконання не дійде, кеш лишається як був).
/// Уся матриця «яка команда що скидає» — в одному місці.
/// </summary>
public sealed class CacheInvalidationBehavior<TRequest, TResponse>(ICacheService cache)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var response = await next(ct);

        switch (request)
        {
            case CreateReviewCommand:
                cache.RemoveGroup(ReviewsCache.ListsGroup, "ReviewCreated");
                break;

            case UpdateReviewCommand c:
                Invalidate(c.Id, "ReviewUpdated");     
                break;

            case DeleteReviewCommand c:
                Invalidate(c.Id, "ReviewDeleted");
                break;

            case AddCommentCommand c:
                Invalidate(c.ReviewId, "CommentAdded");
                break;

            case RemoveCommentCommand c:
                Invalidate(c.ReviewId, "CommentRemoved");
                break;
        }

        return response;
    }

    private void Invalidate(string reviewId, string reason)
    {
        cache.Remove(ReviewsCache.Review(reviewId), reason);
        cache.RemoveGroup(ReviewsCache.ListsGroup, reason);
    }
}
