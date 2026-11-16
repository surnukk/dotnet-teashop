namespace Reviews.Infrastructure.Persistence;

/// <summary>Оброблена подія. Id = EventId; унікальність гарантує сам _id.</summary>
public class ProcessedEventDocument
{
    public string Id { get; set; } = "";
    public DateTime ProcessedAt { get; set; }
}

/// <summary>Остання застосована назва товару й час події, що її встановила (захист від out-of-order).</summary>
public class ProductNameSnapshot
{
    public long Id { get; set; }                    // ProductId
    public string ProductName { get; set; } = "";
    public DateTime UpdatedAt { get; set; }
}