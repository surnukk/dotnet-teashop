using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Reviews.WebApi.Metrics;

/// <summary>Бізнес-метрики сервісу відгуків (Singleton).</summary>
public sealed class ReviewMetrics
{
    public const string MeterName = "Reviews.Api";

    private readonly Counter<long> _created;
    private readonly Counter<long> _updated;
    private readonly Counter<long> _deleted;
    private readonly Counter<long> _commentsAdded;
    private readonly Counter<long> _errors;
    private readonly Histogram<double> _duration;
    private int _inFlight;

    public ReviewMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        _created = meter.CreateCounter<long>("reviews.created", unit: "{review}", description: "Number of created reviews");
        _updated = meter.CreateCounter<long>("reviews.updated", unit: "{review}", description: "Number of updated reviews");
        _deleted = meter.CreateCounter<long>("reviews.deleted", unit: "{review}", description: "Number of deleted reviews");
        _commentsAdded = meter.CreateCounter<long>("reviews.comments.added", unit: "{comment}", description: "Number of added comments");
        _errors = meter.CreateCounter<long>("reviews.errors", unit: "{error}", description: "Failed review operations");

        _duration = meter.CreateHistogram<double>("reviews.operation.duration", unit: "ms",
            description: "Duration of review operations");

        meter.CreateObservableGauge("reviews.operations.in_flight", () => Volatile.Read(ref _inFlight),
            unit: "{operation}", description: "Review operations being executed right now");
    }

    /// <param name="rating">оцінка 1–5 (обмежена кількість значень, тому безпечна як тег)</param>
    public void ReviewCreated(int rating) => _created.Add(1, Tag("review.rating", rating));
    public void ReviewUpdated() => _updated.Add(1);
    public void ReviewDeleted() => _deleted.Add(1);
    public void CommentAdded() => _commentsAdded.Add(1);

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