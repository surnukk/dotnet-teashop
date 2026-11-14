using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Orders.Api.Metrics;

/// <summary>Бізнес-метрики сервісу замовлень (Singleton). Meter створюється через IMeterFactory.</summary>
public sealed class OrderMetrics
{
    public const string MeterName = "Orders.Api"; 

    private readonly Counter<long> _created;
    private readonly Counter<long> _confirmed;
    private readonly Counter<long> _cancelled;
    private readonly Counter<long> _shipped;
    private readonly Counter<long> _errors;
    private readonly Histogram<double> _duration;
    private int _inFlight;

    public OrderMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        _created = meter.CreateCounter<long>("orders.created", unit: "{order}", description: "Number of created orders");
        _confirmed = meter.CreateCounter<long>("orders.confirmed", unit: "{order}", description: "Number of confirmed orders");
        _cancelled = meter.CreateCounter<long>("orders.cancelled", unit: "{order}", description: "Number of cancelled orders");
        _shipped = meter.CreateCounter<long>("orders.shipped", unit: "{order}", description: "Number of shipped orders");
        _errors = meter.CreateCounter<long>("orders.errors", unit: "{error}", description: "Failed order operations");

        _duration = meter.CreateHistogram<double>("orders.operation.duration", unit: "ms",
            description: "Duration of order operations");

        meter.CreateObservableGauge("orders.operations.in_flight", () => Volatile.Read(ref _inFlight),
            unit: "{operation}", description: "Order operations being executed right now");
    }

    // ---- лічильники (збільшуються одразу після успішної операції) ----
    public void OrderCreated(string status, bool withPromotion) =>
        _created.Add(1, Tag("order.status", status), Tag("order.promotion", withPromotion ? "yes" : "no"));

    public void OrderConfirmed() => _confirmed.Add(1, Tag("order.status", "StockConfirmed"));
    public void OrderCancelled() => _cancelled.Add(1, Tag("order.status", "Cancelled"));
    public void OrderShipped() => _shipped.Add(1, Tag("order.status", "Shipped"));

    // ---- вимірювання тривалості: записується і для успішних, і для невдалих операцій ----
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