using System.Diagnostics.CodeAnalysis;
using Platform.EventBus.Events;

namespace Platform.EventBus.Consuming;

public sealed record EventHandlerRegistration(
    Type EventType,
    Func<IServiceProvider, IntegrationEvent, CancellationToken, Task> Handle);

public sealed class EventHandlerRegistry
{
    private readonly Dictionary<string, EventHandlerRegistration> _map;

    public EventHandlerRegistry(IEnumerable<EventHandlerRegistration> registrations)
        => _map = registrations.ToDictionary(r => r.EventType.Name, StringComparer.Ordinal);

    public bool TryGet(string? eventTypeName, [NotNullWhen(true)] out EventHandlerRegistration? registration)
    {
        registration = null;
        return eventTypeName is not null && _map.TryGetValue(eventTypeName, out registration);
    }
}