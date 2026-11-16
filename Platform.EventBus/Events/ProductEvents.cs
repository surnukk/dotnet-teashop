namespace Platform.EventBus.Events;

public record ProductCreatedEvent(int ProductId, string ProductName, decimal Price) : IntegrationEvent
{
    public override IReadOnlyDictionary<string, object?> GetLogContext() =>
        new Dictionary<string, object?> { ["ProductId"] = ProductId };
}

public record ProductPriceChangedEvent(int ProductId, string ProductName, decimal OldPrice, decimal NewPrice) : IntegrationEvent
{
    public override IReadOnlyDictionary<string, object?> GetLogContext() =>
        new Dictionary<string, object?> { ["ProductId"] = ProductId };
}

public record ProductRenamedEvent(int ProductId, string OldName, string NewName) : IntegrationEvent
{
    public override IReadOnlyDictionary<string, object?> GetLogContext() =>
        new Dictionary<string, object?> { ["ProductId"] = ProductId };
}

public record ProductDeletedEvent(int ProductId, string ProductName) : IntegrationEvent
{
    public override IReadOnlyDictionary<string, object?> GetLogContext() =>
        new Dictionary<string, object?> { ["ProductId"] = ProductId };
}