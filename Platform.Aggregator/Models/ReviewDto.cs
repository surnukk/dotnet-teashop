namespace Platform.Aggregator.Models;

public record ReviewDto(
    string Id,
    long ProductId,
    string ProductName,
    string AuthorName,
    int Rating,
    string Text,
    int CommentsCount);

// Reviews повертає відгуки сторінками
public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, long TotalCount);