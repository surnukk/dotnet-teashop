using Microsoft.Extensions.DependencyInjection;
using Platform.EventBus.Abstractions;
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
}