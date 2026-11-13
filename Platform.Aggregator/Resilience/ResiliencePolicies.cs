using Microsoft.Extensions.Http.Resilience;

namespace Platform.Aggregator.Resilience;

/// <summary>1.6: політики для критичних і некритичних сервісів. Решту значень беруть з AggregatorResilienceSetup.</summary>
public static class ResiliencePolicies
{
    /// <summary>Критичний сервіс (Каталог): без товарів картку замовлення не зібрати.</summary>
    public static void Critical(HttpStandardResilienceOptions o)
    {
        o.Retry.MaxRetryAttempts = 4;                                 
        o.Retry.Delay = TimeSpan.FromMilliseconds(300);                
        o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(3);
        o.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(22);
        o.CircuitBreaker.FailureRatio = 0.3;                     
    }

    /// <summary>Некритичний сервіс (Відгуки): Aggregator і без них поверне часткову відповідь.</summary>
    public static void NonCritical(HttpStandardResilienceOptions o)
    {
        o.Retry.MaxRetryAttempts = 1;                                  
        o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(2);            
        o.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(5);  
        o.CircuitBreaker.FailureRatio = 0.7;     
    }
}