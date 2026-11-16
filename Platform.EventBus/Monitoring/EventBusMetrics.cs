using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace Platform.EventBus.Monitoring;

/// <summary>Метрики подієвої шини (Singleton). Meter створюється через IMeterFactory.</summary>
public sealed class EventBusMetrics
{
    public const string MeterName = "Platform.EventBus";

    private readonly Counter<long> _processed;
    private readonly ConcurrentDictionary<string, long> _dlqDepth = new();

    public EventBusMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        _processed = meter.CreateCounter<long>("eventbus.messages.processed", unit: "{message}",
            description: "Consumed messages by outcome (acked, ignored, retried, dead_lettered)");

        meter.CreateObservableGauge<long>("eventbus.dlq.depth", ObserveDlqDepth, unit: "{message}",
            description: "Messages currently waiting in dead letter queues");
    }

    public void Outcome(string queue, string outcome) =>
        _processed.Add(1,
            new KeyValuePair<string, object?>("queue", queue),
            new KeyValuePair<string, object?>("outcome", outcome));

    public void SetDlqDepth(string dlq, long depth) => _dlqDepth[dlq] = depth;

    private IEnumerable<Measurement<long>> ObserveDlqDepth() =>
        _dlqDepth.Select(kv => new Measurement<long>(kv.Value,
            new KeyValuePair<string, object?>("queue", kv.Key)));
}