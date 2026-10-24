using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Reviews.Application.Common.Behaviors;
public class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var name = typeof(TRequest).Name;
        logger.LogInformation("Handling {RequestName} {@Request}", name, request);

        try
        {
            var response = await next(ct);
            logger.LogInformation("Handled {RequestName}", name);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogWarning("Request {RequestName} failed: {ExceptionType} - {Message}",
                name, ex.GetType().Name, ex.Message);
            throw;
        }
    }
}