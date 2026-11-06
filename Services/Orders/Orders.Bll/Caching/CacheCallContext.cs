namespace Orders.Bll.Caching;
public sealed class CacheCallContext
{
    private static readonly AsyncLocal<CacheCallContext?> Holder = new();

    private long _l1Hits, _l2Hits, _misses;

    public static CacheCallContext? Current => Holder.Value;

    public static CacheCallContext Begin()
    {
        var context = new CacheCallContext();
        Holder.Value = context;
        return context;
    }

    public long L1Hits => Interlocked.Read(ref _l1Hits);
    public long L2Hits => Interlocked.Read(ref _l2Hits);
    public long Misses => Interlocked.Read(ref _misses);

    public string Result => (L1Hits + L2Hits, Misses) switch
    {
        (0, 0) => "none",
        (_, 0) => "hit",
        (0, _) => "miss",
        _ => "partial"
    };

    internal void RecordL1Hit() => Interlocked.Increment(ref _l1Hits);
    internal void RecordL2Hit() => Interlocked.Increment(ref _l2Hits);
    internal void RecordMiss() => Interlocked.Increment(ref _misses);
}