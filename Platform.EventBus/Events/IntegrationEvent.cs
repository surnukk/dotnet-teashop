namespace Platform.EventBus.Events;

public abstract record IntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public string? CorrelationId { get; init; }
    public int SchemaVersion { get; init; } = 1;

    private static readonly IReadOnlyDictionary<string, object?> EmptyContext = new Dictionary<string, object?>();

    /// <summary>Ідентифікатори сутностей для структурованих логів обробки.</summary>
    public virtual IReadOnlyDictionary<string, object?> GetLogContext() => EmptyContext;
}