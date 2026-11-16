namespace Platform.EventBus.Consuming;

/// <summary>
/// Постійний збій обробки (наприклад, некоректні бізнес-дані): повтор не допоможе,
/// повідомлення одразу йде в DLQ.
/// </summary>
public sealed class PermanentFailureException(string message, Exception? inner = null)
    : Exception(message, inner);