using System.Diagnostics;
using Catalog.Contracts;
using Grpc.Core;
using Orders.Contracts;
using Platform.Aggregator.Models;
using Reviews.Contracts;
using Platform.Aggregator.Caching;

namespace Platform.Aggregator.Services;

public class OrderAggregator(OrdersService.OrdersServiceClient orders, CatalogService.CatalogServiceClient catalog,
    ReviewsService.ReviewsServiceClient reviews, AggregateCache cache, ILogger<OrderAggregator> logger)
{
    private const int ReviewsPageSize = 20;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    public async Task<FullOrderDto?> GetFullOrderAsync(long orderId, CancellationToken ct)
    {
        var key = AggregateCache.FullOrderKey(orderId);

        var cached = await cache.GetAsync<FullOrderDto>(key, ct);
        if (cached is not null)
        {
            logger.LogInformation("Aggregate cache hit {Key}", key);
            return cached;
        }

        logger.LogInformation("Aggregate cache miss {Key}", key);
        var result = await BuildFullOrderAsync(orderId, ct);

        if (result is not null && result.Warnings.Count == 0)
        {
            await cache.SetAsync(key, result, CacheTtl, ct);
            logger.LogInformation("Aggregate cache set {Key}, ttl {TtlSeconds} s", key, (int)CacheTtl.TotalSeconds);
        }

        return result;
    }

    private async Task<FullOrderDto?> BuildFullOrderAsync(long orderId, CancellationToken ct)
    {
        var warnings = new List<string>();

        var sw = Stopwatch.StartNew();
        OrderReply orderReply;
        try
        {
            orderReply = await orders.GetOrderAsync(new GetOrderRequest { Id = orderId }, cancellationToken: ct);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            logger.LogInformation("orders-api: order {OrderId} not found", orderId);
            return null;
        }
        logger.LogInformation("orders-api responded in {Elapsed} ms", sw.ElapsedMilliseconds);

        var order = ToOrder(orderReply);
        var productIds = order.Items.Select(i => i.ProductId).Distinct().ToList();
        if (productIds.Count == 0)
            return new FullOrderDto(order, [], [], warnings);

        var productsTask = SafeAsync(
            () => catalog.GetProductsByIdsAsync(
                new GetProductsByIdsRequest { Ids = { productIds } }, cancellationToken: ct).ResponseAsync,
            "catalog-api", warnings, ct);

        var reviewsTask = SafeAsync(
            () => reviews.GetForProductsAsync(
                new GetForProductsRequest { ProductIds = { productIds }, PageSize = ReviewsPageSize },
                cancellationToken: ct).ResponseAsync,
            "reviews-api", warnings, ct);

        await Task.WhenAll(productsTask, reviewsTask);      

        var products = (await productsTask)?.Products.Select(ToProduct).ToList(); 
        var reviewList = (await reviewsTask)?.Reviews.Select(ToReview).ToList();

        var orderProducts = (products ?? []).Where(p => productIds.Contains(p.ProductId)).ToList();
        var orderReviews = (reviewList ?? []).Where(r => productIds.Contains(r.ProductId)).ToList();

        if (products is not null)
        {
            foreach (var missingId in productIds.Where(id => orderProducts.All(p => p.ProductId != id)))
                warnings.Add($"Product {missingId} was not found in catalog-api");
        }

        return new FullOrderDto(order, orderProducts, orderReviews, warnings);
    }

    private async Task<T?> SafeAsync<T>(
        Func<Task<T>> call, string service, List<string> warnings, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await call();
            logger.LogInformation("{Service} responded in {Elapsed} ms", service, sw.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex) when (IsServiceFailure(ex) && !ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "{Service} is unavailable", service);
            lock (warnings) warnings.Add($"{service} is unavailable");
            return default;
        }
    }

    internal static bool IsServiceFailure(Exception ex) => ex is
        RpcException
        {
            StatusCode: StatusCode.Unavailable or StatusCode.DeadlineExceeded
                                   or StatusCode.Internal or StatusCode.Unknown
        }
        or HttpRequestException
        or Polly.Timeout.TimeoutRejectedException
        or Polly.CircuitBreaker.BrokenCircuitException;

    private static OrderDto ToOrder(OrderReply o) => new(
        o.OrderId,
        o.CustomerId,
        o.HasPromotionId ? o.PromotionId : (long?)null,     
        o.OrderDate.ToDateTime(),                       
        o.OrderState,
        (decimal)o.TotalAmount,                           
        o.Items.Select(i => new OrderItemDto(
            i.OrderId, i.ProductId, i.ProductName, (decimal)i.UnitPrice, i.Units)).ToList());

    private static ProductDto ToProduct(ProductReply p) => new(
        p.Id, p.Name, p.CategoryName, (decimal)p.Price, p.StockQuantity, p.IsAvailable);

    private static ReviewDto ToReview(ReviewReply r) => new(
        r.Id, r.ProductId, r.ProductName, r.AuthorName, r.Rating, r.Text, r.CommentsCount);
}