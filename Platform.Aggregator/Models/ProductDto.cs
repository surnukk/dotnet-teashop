namespace Platform.Aggregator.Models;

public record ProductDto(
    long ProductId,
    string ProductName,
    string CategoryName,
    decimal Price,
    int StockQuantity,
    bool IsAvailable);