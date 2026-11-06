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
				await cache.RemoveGroupAsync(ReviewsCache.ListsGroup, "ReviewCreated", ct);
				break;
			case UpdateReviewCommand c:
				await InvalidateAsync(c.Id, "ReviewUpdated", ct);
				break;
			case DeleteReviewCommand c:
				await InvalidateAsync(c.Id, "ReviewDeleted", ct);
				break;
			case AddCommentCommand c:
				await InvalidateAsync(c.ReviewId, "CommentAdded", ct);
				break;
			case RemoveCommentCommand c:
				await InvalidateAsync(c.ReviewId, "CommentRemoved", ct);
				break;
		}

		return response;
	}

	private async Task InvalidateAsync(string reviewId, string reason, CancellationToken ct)
	{
		await cache.RemoveAsync(ReviewsCache.Review(reviewId), reason, ct);
		await cache.RemoveGroupAsync(ReviewsCache.ListsGroup, reason, ct);
	}
}