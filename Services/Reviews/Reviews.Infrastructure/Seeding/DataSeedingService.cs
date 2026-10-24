using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Reviews.Infrastructure.Seeding;

public class DataSeedingService(IServiceProvider services, ILogger<DataSeedingService> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        try
        {
            using var scope = services.CreateScope();

            foreach (var seeder in scope.ServiceProvider.GetServices<IDataSeeder>())
                await seeder.SeedAsync(ct);
        }
        catch (Exception ex) when (ex is MongoException or TimeoutException)
        {
            logger.LogError(ex, "Data seeding failed");
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}