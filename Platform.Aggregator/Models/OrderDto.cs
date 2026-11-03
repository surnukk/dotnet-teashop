namespace Platform.Aggregator.Models;

public record OrderItemDto(long OrderID, long ProductId, string ProductName, decimal UnitPrice, int Units);

public record OrderDto(
    long OrderID,
    long CustomerID,
    long? PromotionID,
    DateTime OrderDate,
    string OrderState,
    decimal TotalAmount,
    IReadOnlyList<OrderItemDto> Items);