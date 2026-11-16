using Microsoft.Extensions.DependencyInjection;
using Platform.EventBus.Topology;

namespace Platform.EventBus;

public static class EventBusServiceCollectionExtensions
{
    public static IServiceCollection AddEventBusTopology(this IServiceCollection services)
    {
        services.AddHostedService<RabbitMqTopologyInitializer>();
        return services;
    }
}