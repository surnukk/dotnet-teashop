using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Api.Common;

public class AuthExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        if (exception is not AuthException ex)
            return false;   // інші винятки → стандартна відповідь 500

        var problem = new ProblemDetails { Status = ex.StatusCode, Title = ex.Message };
        if (ex.Errors is not null)
            problem.Extensions["errors"] = ex.Errors;

        context.Response.StatusCode = ex.StatusCode;
        await context.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json", ct);
        return true;
    }
}