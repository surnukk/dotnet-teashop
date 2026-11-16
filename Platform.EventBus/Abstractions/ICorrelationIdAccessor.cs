namespace Platform.EventBus.Abstractions;

public interface ICorrelationIdAccessor
{
    string? Current { get; }
}