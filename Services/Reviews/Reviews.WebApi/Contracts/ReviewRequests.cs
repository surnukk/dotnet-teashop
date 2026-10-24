namespace Reviews.WebApi.Contracts;

public record CreateReviewRequest(
    long ProductId,
    string ProductName,
    string AuthorId,
    string AuthorName,
    int Rating,
    string Text);

public record UpdateReviewRequest(int Rating, string Text);

public record AddCommentRequest(string AuthorId, string AuthorName, string Text);