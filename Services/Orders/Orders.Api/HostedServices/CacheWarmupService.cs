using System.Diagnostics;
using Orders.Bll.Services;

namespace Orders.Api.HostedServices;

public sealed class CacheWarmupService(IServiceScopeFactory scopes, ILogger<CacheWarmupService> logger)
    : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await WarmAsync("startup", stoppingToken);

        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await WarmAsync("periodic", stoppingToken);
    }

    private async Task WarmAsync(string reason, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var products = scope.ServiceProvider.GetRequiredService<IProductService>();
            var promotions = scope.ServiceProvider.GetRequiredService<IPromotionService>();

            var productsCount = (await products.GetAllAsync(ct)).Count();
            var promotionsCount = (await promotions.GetActivePromotionsAsync(ct)).Count();

            logger.LogInformation(
                "Cache warm-up ({Reason}) finished in {ElapsedMs} ms: {Products} products, {Promotions} active promotions",
                reason, sw.ElapsedMilliseconds, productsCount, promotionsCount);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache warm-up ({Reason}) failed, will retry in {Minutes} min",
                reason, Interval.TotalMinutes);
        }
    }
}