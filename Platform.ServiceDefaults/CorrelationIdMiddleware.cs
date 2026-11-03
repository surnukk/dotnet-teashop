using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace Microsoft.Extensions.Hosting;

public class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        string correlationId = context.Request.Headers[HeaderName].ToString();
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
            context.Request.Headers[HeaderName] = correlationId;   // далі його побачать YARP і HttpClient
        }

        context.Response.Headers[HeaderName] = correlationId;      // повертаємо клієнту
        Activity.Current?.SetTag("correlation.id", correlationId); // видно і в Traces

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            logger.LogInformation("Request started {Method} {Path}",
                context.Request.Method, context.Request.Path);

            await next(context);

            logger.LogInformation("Request finished with {StatusCode}", context.Response.StatusCode);
        }
    }
}