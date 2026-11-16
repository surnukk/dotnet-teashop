namespace Platform.EventBus.Events;

public record OrderStatusChangedEvent(long OrderId, string OldStatus, string NewStatus)
    : IntegrationEvent;