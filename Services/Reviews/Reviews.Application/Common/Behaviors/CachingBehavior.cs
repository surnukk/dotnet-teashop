using MediatR;
using Reviews.Application.Common.Caching;

namespace Reviews.Application.Common.Behaviors;

/// <summary>
/// Cache-aside для запитів: кеш → handler (MongoDB) → кеш.
/// Застосовується лише до запитів, що реалізують ICacheableQuery (для решти MediatR цей behavior пропускає).
/// Стоїть у конвеєрі ПІСЛЯ ValidationBehavior: некоректний Id відсікається ще до звернення до кешу.
/// </summary>
public sealed class CachingBehavior<TRequest, TResponse>(ICacheService cache)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, ICacheableQuery
    where TResponse : class
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var result = await cache.GetOrCreateAsync<TResponse>(
            request.CacheKey,
            async token => await next(token),
            request.CachePolicy,
            ct);

        return result!;
    }
}
