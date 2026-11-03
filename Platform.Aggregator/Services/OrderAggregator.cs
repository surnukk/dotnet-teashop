using System.Diagnostics;
using Platform.Aggregator.Clients;
using Platform.Aggregator.Models;

namespace Platform.Aggregator.Services;

public class OrderAggregator(
    OrdersClient orders,
    CatalogClient catalog,
    ReviewsClient reviews,
    ILogger<OrderAggregator> logger)
{
    public async Task<FullOrderDto?> GetFullOrderAsync(long orderId, CancellationToken ct)
    {
        var warnings = new List<string>();

        // 1) без замовлення агрегувати нічого: якщо orders-api недоступний, помилка йде нагору
        var sw = Stopwatch.StartNew();
        var order = await orders.GetOrderAsync(orderId, ct);
        logger.LogInformation("orders-api responded in {Elapsed} ms", sw.ElapsedMilliseconds);
        if (order is null) return null;

        var productIds = order.Items.Select(i => i.ProductId).Distinct().ToList();

        // 2) товари й відгуки не залежать один від одного → запускаємо одночасно
        var productsTask = SafeAsync(() => catalog.GetProductsAsync(productIds, ct), "catalog-api", warnings, ct);
        var reviewsTask = SafeAsync(() => reviews.GetForProductsAsync(productIds, ct), "reviews-api", warnings, ct);

        await Task.WhenAll(productsTask, reviewsTask);                // паралельно, без .Result і WaitAll

        var products = await productsTask;
        var reviewList = await reviewsTask;

        // 3) перевірка узгодженості: у відповіді лише товари й відгуки саме цього замовлення
        var orderProducts = (products ?? []).Where(p => productIds.Contains(p.ProductId)).ToList();
        var orderReviews = (reviewList ?? []).Where(r => productIds.Contains(r.ProductId)).ToList();

        if (products is not null)
        {
            foreach (var missingId in productIds.Where(id => orderProducts.All(p => p.ProductId != id)))
                warnings.Add($"Product {missingId} was not found in catalog-api");
        }

        return new FullOrderDto(order, orderProducts, orderReviews, warnings);
    }

    // виконує виклик; якщо сервіс недоступний — не валить запит, а додає попередження
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
        catch (Exception ex) when ((ex is HttpRequestException or TaskCanceledException or Polly.Timeout.TimeoutRejectedException or Polly.CircuitBreaker.BrokenCircuitException)
            && !ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "{Service} is unavailable", service);
            lock (warnings) warnings.Add($"{service} is unavailable");
            return default;
        }
    }
}