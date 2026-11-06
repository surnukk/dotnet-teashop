using System.Diagnostics;
using Catalog.Bll.DTO;
using Catalog.Bll.Services;

namespace Catalog.Api.HostedServices;

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
            var categories = scope.ServiceProvider.GetRequiredService<ICategoryService>();
            var tags = scope.ServiceProvider.GetRequiredService<ITagService>();
            var products = scope.ServiceProvider.GetRequiredService<IProductService>();

            var categoriesCount = (await categories.GetAllAsync(ct)).Count;
            var tagsCount = (await tags.GetAllAsync(ct)).Count;
            var firstPage = await products.GetListAsync(new ProductQuery(), ct);   // перша сторінка каталогу

            logger.LogInformation(
                "Cache warm-up ({Reason}) finished in {ElapsedMs} ms: {Categories} categories, {Tags} tags, {Products} products on first page",
                reason, sw.ElapsedMilliseconds, categoriesCount, tagsCount, firstPage.Items.Count);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache warm-up ({Reason}) failed, will retry in {Minutes} min",
                reason, Interval.TotalMinutes);
        }
    }
}