using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;

namespace Platform.Aggregator.Resilience;

/// <summary>
/// Базові resilience-налаштування для всіх клієнтів Aggregator (1.3–1.5).
/// Клієнти з іншими вимогами (Каталог, Відгуки) перевизначають окремі значення (1.6).
/// </summary>
internal sealed class AggregatorResilienceSetup(ILoggerFactory loggerFactory)
    : IConfigureNamedOptions<HttpStandardResilienceOptions>
{
    public void Configure(HttpStandardResilienceOptions options) => Configure(Options.DefaultName, options);

    public void Configure(string? name, HttpStandardResilienceOptions o)
    {
        var logger = loggerFactory.CreateLogger("Platform.Aggregator.Resilience");
        var pipeline = string.IsNullOrEmpty(name) ? "default" : name;

        // ---- 1.3 Retry ----
        o.Retry.MaxRetryAttempts = 2;                 
        o.Retry.Delay = TimeSpan.FromMilliseconds(500);        
        o.Retry.BackoffType = DelayBackoffType.Exponential;  
        o.Retry.UseJitter = true;                             

        // ---- 1.5 Таймаути ----
        o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(3);  
        o.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(12);

        // ---- 1.4 Circuit Breaker ----
        o.CircuitBreaker.FailureRatio = 0.5;                
        o.CircuitBreaker.MinimumThroughput = 10;               
        o.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
        o.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);     

        o.CircuitBreaker.OnOpened = args =>
        {
            logger.LogError(
                "Circuit breaker OPENED for {Pipeline}: requests are rejected for {BreakSeconds} s, reason {Reason}",
                pipeline, (int)args.BreakDuration.TotalSeconds,
                args.Outcome.Exception?.GetType().Name ?? args.Outcome.Result?.StatusCode.ToString());
            return ValueTask.CompletedTask;
        };
        o.CircuitBreaker.OnHalfOpened = _ =>
        {
            logger.LogWarning("Circuit breaker HALF-OPEN for {Pipeline}: trial request is allowed", pipeline);
            return ValueTask.CompletedTask;
        };
        o.CircuitBreaker.OnClosed = _ =>
        {
            logger.LogInformation("Circuit breaker CLOSED for {Pipeline}: service recovered", pipeline);
            return ValueTask.CompletedTask;
        };
    }
}