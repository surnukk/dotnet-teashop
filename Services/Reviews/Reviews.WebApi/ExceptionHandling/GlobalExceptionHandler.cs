using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Reviews.Domain.Exceptions;

namespace Reviews.WebApi.ExceptionHandling;

public class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        var (status, title, detail) = Describe(exception);

        if (status >= StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Request failed with {Status}: {ExceptionType}",
                status, exception.GetType().Name);
        else
            logger.LogWarning("Request rejected with {Status}: {ExceptionType} - {Message}",
                status, exception.GetType().Name, exception.Message);

        ProblemDetails problem = exception is ValidationException validation
            ? new HttpValidationProblemDetails(GroupErrors(validation))
            { Status = status, Title = title, Detail = detail }
            : new ProblemDetails { Status = status, Title = title, Detail = detail };

        httpContext.Response.StatusCode = status;

        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });

        return true;
    }

    private static (int Status, string Title, string Detail) Describe(Exception ex) => ex switch
    {
        NotFoundException
            => (StatusCodes.Status404NotFound, "Not Found", ex.Message),

        ValidationException
            => (StatusCodes.Status400BadRequest, "Validation failed",
                "One or more validation errors occurred."),

        DomainException
            => (StatusCodes.Status400BadRequest, "Business rule violated", ex.Message),

        ConflictException                                  // у т. ч. конфлікт версій
            => (StatusCodes.Status409Conflict, "Conflict", ex.Message),

        MongoWriteException { WriteError.Category: ServerErrorCategory.DuplicateKey }
            => (StatusCodes.Status409Conflict, "Conflict",
                "A document with the same unique value already exists (e.g. this author has already reviewed this product)."),

        MongoWriteException                                // інші помилки запису
            => (StatusCodes.Status400BadRequest, "Write failed",
                "The database rejected the write operation."),

        MongoConnectionException or TimeoutException      // Mongo недоступна
            => (StatusCodes.Status503ServiceUnavailable, "Service Unavailable",
                "The database is temporarily unavailable. Please try again later."),

        _ => (StatusCodes.Status500InternalServerError, "Internal Server Error",
              "An unexpected error occurred.")             // деталі не витікають клієнту
    };

    private static Dictionary<string, string[]> GroupErrors(ValidationException ex)
        => ex.Errors
            .GroupBy(e => string.IsNullOrEmpty(e.PropertyName) ? "request" : e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());
}