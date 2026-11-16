using Microsoft.AspNetCore.Http;
using Platform.EventBus.Abstractions;

namespace Platform.EventBus.Publishing;

public sealed class HttpContextCorrelationIdAccessor(IHttpContextAccessor accessor) : ICorrelationIdAccessor
{
    private const string HeaderName = "X-Correlation-Id";

    public string? Current
    {
        get
        {
            var value = accessor.HttpContext?.Request.Headers[HeaderName].ToString();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }
}