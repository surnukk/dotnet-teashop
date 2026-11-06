using Microsoft.Extensions.Caching.Memory;

namespace Reviews.Application.Common.Caching;

public static class ReviewsCache
{
    public const string ListsGroup = "reviews:group:lists";  

    public static string Review(string id) => $"reviews:review:{id.ToLowerInvariant()}";

    public static string List(long? productId, int page, int pageSize)
        => $"reviews:list:{(productId is null ? "all" : productId.ToString())}:p{page}:s{pageSize}";
}

public static class ReviewsCachePolicies
{
    public static readonly CacheEntryPolicy Review = new(
        Absolute: TimeSpan.FromMinutes(10),
        Sliding: TimeSpan.FromMinutes(2));

    public static readonly CacheEntryPolicy List = new(
        Absolute: TimeSpan.FromMinutes(2),
        Sliding: TimeSpan.FromMinutes(1),
        Group: ReviewsCache.ListsGroup,
        Priority: CacheItemPriority.Low);
}
