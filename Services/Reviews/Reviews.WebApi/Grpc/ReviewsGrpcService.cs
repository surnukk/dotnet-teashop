using Grpc.Core;
using MediatR;
using Reviews.Application.Common.Models;
using Reviews.Application.Queries.GetReviewsList;
using Reviews.Contracts;

namespace Reviews.WebApi.GrpcServices;

/// <summary>gRPC-адаптер над застосунком Reviews: ті самі MediatR-запити, що й у REST-контролері.</summary>
public class ReviewsGrpcService(ISender mediator, ILogger<ReviewsGrpcService> logger)
    : ReviewsService.ReviewsServiceBase
{
    private const int MaxProducts = 100;
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public override Task<ReviewsReply> GetForProducts(GetForProductsRequest request, ServerCallContext context)
        => GrpcCall.RunAsync(nameof(GetForProducts), context, logger, async ct =>
        {
            if (request.ProductIds.Count == 0)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "product_ids must not be empty"));
            if (request.ProductIds.Count > MaxProducts)
                throw new RpcException(new Status(StatusCode.InvalidArgument, $"At most {MaxProducts} product ids per call"));
            if (request.ProductIds.Any(id => id <= 0))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "All product ids must be positive"));
            if (request.PageSize < 0 || request.PageSize > MaxPageSize)
                throw new RpcException(new Status(StatusCode.InvalidArgument, $"page_size must be between 0 and {MaxPageSize}"));

            var pageSize = request.PageSize == 0 ? DefaultPageSize : request.PageSize;
            var reply = new ReviewsReply();

            foreach (var productId in request.ProductIds.Distinct())
            {
                var page = await mediator.Send(new GetReviewsListQuery(productId, 1, pageSize), ct);
                reply.Reviews.AddRange(page.Items.Select(ToReply));
            }

            return reply;
        });

    private static ReviewReply ToReply(ReviewDto r) => new()
    {
        Id = r.Id,
        ProductId = r.ProductId,
        ProductName = r.ProductName ?? string.Empty, 
        AuthorName = r.AuthorName ?? string.Empty,
        Rating = r.Rating,
        Text = r.Text ?? string.Empty,
        CommentsCount = r.CommentsCount
    };
}