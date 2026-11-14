using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Catalog.Api.Metrics;

/// <summary>Бізнес-метрики сервісу каталогу (Singleton).</summary>
public sealed class CatalogMetrics
{
    public const string MeterName = "Catalog.Api";

    private readonly Counter<long> _created;
    private readonly Counter<long> _updated;
    private readonly Counter<long> _deleted;
    private readonly Counter<long> _errors;
    private readonly Histogram<double> _duration;
    private int _inFlight;

    public CatalogMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        _created = meter.CreateCounter<long>("products.created", unit: "{product}", description: "Number of created products");
        _updated = meter.CreateCounter<long>("products.updated", unit: "{product}", description: "Number of product changes (price, stock)");
        _deleted = meter.CreateCounter<long>("products.deleted", unit: "{product}", description: "Number of deleted products");
        _errors = meter.CreateCounter<long>("catalog.errors", unit: "{error}", description: "Failed catalog operations");

        _duration = meter.CreateHistogram<double>("catalog.operation.duration", unit: "ms",
            description: "Duration of catalog operations");

        meter.CreateObservableGauge("catalog.operations.in_flight", () => Volatile.Read(ref _inFlight),
            unit: "{operation}", description: "Catalog operations being executed right now");
    }

    public void ProductCreated() => _created.Add(1);

    /// <param name="change">price | stock_added | stock_removed</param>
    public void ProductUpdated(string change) => _updated.Add(1, Tag("product.change", change));

    public void ProductDeleted() => _deleted.Add(1);

    public async Task<T> MeasureAsync<T>(string operation, Func<Task<T>> action)
    {
        Interlocked.Increment(ref _inFlight);
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await action();
            RecordDuration(operation, success: true, sw.Elapsed.TotalMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            RecordDuration(operation, success: false, sw.Elapsed.TotalMilliseconds);
            _errors.Add(1, Tag("operation", operation), Tag("error.type", ex.GetType().Name));
            throw;
        }
        finally
        {
            Interlocked.Decrement(ref _inFlight);
        }
    }

    public Task MeasureCommandAsync(string operation, Func<Task> action) =>
        MeasureAsync<object?>(operation, async () =>
        {
            await action();
            return null;
        });

    private void RecordDuration(string operation, bool success, double ms) =>
        _duration.Record(ms, Tag("operation", operation), Tag("status", success ? "success" : "failure"));

    private static KeyValuePair<string, object?> Tag(string key, object? value) => new(key, value);
}