using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Reviews.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services, IConfiguration config)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.LicenseKey = config["LuckyPenny:LicenseKey"];
            cfg.RegisterServicesFromAssembly(assembly);

            // Частина 4: розкоментуємо, коли створимо behaviors (порядок = порядок виконання)
            // cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));      // 1 — зовнішній
            // cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));   // 2
            // cfg.AddOpenBehavior(typeof(PerformanceBehavior<,>));  // 3 — біля обробника
        });

        services.AddValidatorsFromAssembly(assembly);
        return services;
    }
}