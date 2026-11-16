using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Platform.EventBus.Abstractions;
using Platform.EventBus.Consuming;
using Platform.EventBus.Events;
using Platform.EventBus.Publishing;
using Platform.EventBus.Topology;

namespace Platform.EventBus;

public static class EventBusServiceCollectionExtensions
{
    public static IServiceCollection AddEventBusTopology(this IServiceCollection services)
    {
        services.AddHostedService<RabbitMqTopologyInitializer>();
        return services;
    }

    public static IServiceCollection AddEventPublisher(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddSingleton<ICorrelationIdAccessor, HttpContextCorrelationIdAccessor>();
        services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
        return services;
    }

    /// <summary>Реєструє обробник (scoped) і прив'язує його до типу події.</summary>
    public static IServiceCollection AddEventHandler<TEvent, THandler>(this IServiceCollection services)
        where TEvent : IntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>
    {
        services.AddScoped<THandler>();
        services.AddSingleton(new EventHandlerRegistration(
            typeof(TEvent),
            (sp, e, ct) => sp.GetRequiredService<THandler>().HandleAsync((TEvent)e, ct)));
        services.TryAddSingleton<EventHandlerRegistry>();
        return services;
    }
}