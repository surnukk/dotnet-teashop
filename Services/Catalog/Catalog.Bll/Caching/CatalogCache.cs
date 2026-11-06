using System.Globalization;
using Microsoft.Extensions.Caching.Memory;

namespace Catalog.Bll.Caching;

public static class CatalogCache
{
    public const string ListsGroup = "catalog:group:lists";    
    public const string ProductCardsGroup = "catalog:group:product-cards";

    public const string Categories = "catalog:categories:all";
    public const string Tags = "catalog:tags:all";

    public static string Product(int id) => $"catalog:product:{id}";
    public static string CategoryWithProducts(int id) => $"catalog:category:{id}:products";
    public static string Tag(int id) => $"catalog:tag:{id}";

    public static string ProductList(
        int page, int pageSize, string? search, int? categoryId, int? tagId,
        decimal? minPrice, decimal? maxPrice, bool? isAvailable, string sortBy, bool descending)
        => string.Create(CultureInfo.InvariantCulture,
            $"catalog:products:list:p{page}:s{pageSize}:q{search?.Trim().ToLowerInvariant()}:c{categoryId}:t{tagId}:min{minPrice}:max{maxPrice}:av{isAvailable}:{sortBy}:{(descending ? "desc" : "asc")}");
}

public static class CatalogCachePolicies
{
    public static readonly CacheEntryPolicy ProductCard = new(
        Absolute: TimeSpan.FromHours(1),
        Sliding: TimeSpan.FromMinutes(5),
        Group: CatalogCache.ProductCardsGroup);

    public static readonly CacheEntryPolicy Reference = new(
        Absolute: TimeSpan.FromMinutes(30),
        Priority: CacheItemPriority.High);

    public static readonly CacheEntryPolicy List = new(
        Absolute: TimeSpan.FromMinutes(2),
        Sliding: TimeSpan.FromMinutes(1),
        Group: CatalogCache.ListsGroup,
        Priority: CacheItemPriority.Low);
}
