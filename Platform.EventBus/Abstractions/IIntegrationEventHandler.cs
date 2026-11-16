using Platform.EventBus.Events;

namespace Platform.EventBus.Abstractions;

public interface IIntegrationEventHandler<in TEvent> where TEvent : IntegrationEvent
{
    Task HandleAsync(TEvent @event, CancellationToken ct);
}