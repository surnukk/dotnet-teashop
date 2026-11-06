using Microsoft.Extensions.Caching.Memory;

namespace Orders.Bll.Caching;

public static class OrdersCache
{
    public const string PromotionsGroup = "orders:group:promotions";
    public const string OrderListsGroup = "orders:group:order-lists";

    public const string Products = "orders:products:all";
    public const string ActivePromotions = "orders:promotions:active";

    public static string Product(long id) => $"orders:product:{id}";
    public static string Promotion(long id) => $"orders:promotion:{id}";
    public static string PromotionByCode(string code) => $"orders:promotion:code:{code.Trim().ToUpperInvariant()}";
    public static string Customer(long id) => $"orders:customer:{id}";
    public static string Order(long id) => $"orders:order:{id}";
    public static string CustomerOrders(long customerId) => $"orders:customer:{customerId}:orders";
    public static string Payment(long orderId) => $"orders:payment:{orderId}";

	public static async Task InvalidateOrderAsync(this ICacheService cache, long orderId, string reason)
	{
		await cache.RemoveAsync(Order(orderId), reason);
		await cache.RemoveAsync(Payment(orderId), reason);
		await cache.RemoveGroupAsync(OrderListsGroup, reason);
	}
}

public static class OrdersCachePolicies
{
    public static readonly CacheEntryPolicy Product = new(
        Absolute: TimeSpan.FromMinutes(15),
        Priority: CacheItemPriority.High);

    public static readonly CacheEntryPolicy Promotion = new(
        Absolute: TimeSpan.FromMinutes(2));

    public static readonly CacheEntryPolicy Customer = new(
        Absolute: TimeSpan.FromMinutes(1),
        Priority: CacheItemPriority.Low);

    public static readonly CacheEntryPolicy Order = new(
        Absolute: TimeSpan.FromMinutes(5),
        Sliding: TimeSpan.FromMinutes(1));

    public static readonly CacheEntryPolicy OrderList = new(
        Absolute: TimeSpan.FromMinutes(1),
        Group: OrdersCache.OrderListsGroup,
        Priority: CacheItemPriority.Low);

    public static readonly CacheEntryPolicy Payment = new(
        Absolute: TimeSpan.FromMinutes(1),
        Priority: CacheItemPriority.Low);
}
