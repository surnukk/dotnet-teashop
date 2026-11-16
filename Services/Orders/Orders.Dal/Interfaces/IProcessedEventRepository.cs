namespace Orders.Dal.Interfaces;

public interface IProcessedEventRepository
{
    /// <summary>true — подію записано вперше; false — вона вже оброблялась (дублікат).</summary>
    Task<bool> TryAddAsync(Guid eventId, CancellationToken ct = default);
}