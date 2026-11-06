namespace Reviews.Application.Common.Caching;

/// <summary>
/// Маркер для запитів (Query), результат яких можна кешувати.
/// CachingBehavior у конвеєрі MediatR сам перевіряє кеш перед викликом handler-а.
/// </summary>
public interface ICacheableQuery
{
    string CacheKey { get; }
    CacheEntryPolicy CachePolicy { get; }
}
