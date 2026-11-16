using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Platform.EventBus.Monitoring;

public sealed record DlqMonitorTargets(IReadOnlyList<string> Queues);

/// <summary>Раз на 15 с читає глибину DLQ і публікує її як gauge-метрику; попереджає в логах, якщо DLQ не порожня.</summary>
public sealed class DlqDepthMonitor(
    IConnection connection,
    DlqMonitorTargets targets,
    EventBusMetrics metrics,
    ILogger<DlqDepthMonitor> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(15);
    private readonly Dictionary<string, long> _last = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        do
        {
            await PollAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task PollAsync(CancellationToken ct)
    {
        try
        {
            await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);

            foreach (var dlq in targets.Queues)
            {
                var info = await channel.QueueDeclarePassiveAsync(dlq, ct);   // лише читає стан, нічого не змінює
                long depth = info.MessageCount;
                metrics.SetDlqDepth(dlq, depth);

                _last.TryGetValue(dlq, out var previous);
                if (depth > 0 && depth != previous)
                    logger.LogWarning(
                        "Dead letter queue {Queue} contains {Depth} message(s): manual investigation required",
                        dlq, depth);
                _last[dlq] = depth;
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read dead letter queue depth");
        }
    }
}