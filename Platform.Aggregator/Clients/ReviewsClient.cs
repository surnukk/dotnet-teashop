using Platform.Aggregator.Models;

namespace Platform.Aggregator.Clients;

public class ReviewsClient(HttpClient http, ILogger<ReviewsClient> logger)
{
    public async Task<IReadOnlyList<ReviewDto>> GetForProductsAsync(
        IEnumerable<long> productIds, CancellationToken ct)
    {
        var tasks = productIds.Select(id => GetForProductAsync(id, ct));
        var perProduct = await Task.WhenAll(tasks);
        return perProduct.SelectMany(x => x).ToList();
    }

    private async Task<IReadOnlyList<ReviewDto>> GetForProductAsync(long productId, CancellationToken ct)
    {
        logger.LogInformation("Calling reviews-api for product {ProductId}", productId);

        var page = await http.GetFromJsonAsync<PagedResult<ReviewDto>>(
            $"/api/reviews?productId={productId}&pageSize=20", ct);
        return page?.Items ?? [];
    }
}