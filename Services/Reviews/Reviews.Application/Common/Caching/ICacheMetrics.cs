namespace Reviews.Application.Common.Caching;

public interface ICacheMetrics
{
    void L1Hit();
    void L2Hit();
    void FullMiss();
    void Invalidated(string scope);
    IDisposable Time(string operation);
}