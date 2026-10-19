using Catalog.Domain;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.ExceptionHandling;

public class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetailsService,
    IHostEnvironment env) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext http, Exception ex, CancellationToken ct)
    {
        var (status, title) = ex switch
        {
            EntityNotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            ValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            DomainException => (StatusCodes.Status409Conflict, "Business rule violation"),
            _ => (StatusCodes.Status500InternalServerError, "Internal server error")
        };

        if (status == StatusCodes.Status500InternalServerError)
            logger.LogError(ex, "Request {Path} failed", http.Request.Path);
        else
            logger.LogWarning(ex, "Request {Path} failed with {Status}", http.Request.Path, status);

        ProblemDetails problem = ex is ValidationException ve
            ? new ValidationProblemDetails(ve.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()))
            : new ProblemDetails();

        problem.Status = status;
        problem.Title = title;
        problem.Instance = http.Request.Path;
        problem.Detail = status switch
        {
            StatusCodes.Status500InternalServerError =>
                env.IsDevelopment() ? ex.ToString() : "An unexpected error occurred.",
            _ => ex.Message
        };

        http.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            ProblemDetails = problem,
            Exception = ex
        });
    }
}