namespace Platform.EventBus.Events;

public record ProductCreatedEvent(int ProductId, string ProductName, decimal Price)
    : IntegrationEvent;

public record ProductPriceChangedEvent(int ProductId, string ProductName, decimal OldPrice, decimal NewPrice)
    : IntegrationEvent;

public record ProductRenamedEvent(int ProductId, string OldName, string NewName)
    : IntegrationEvent;

public record ProductDeletedEvent(int ProductId, string ProductName)
    : IntegrationEvent;