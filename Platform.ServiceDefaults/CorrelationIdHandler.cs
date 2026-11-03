using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Extensions.Hosting;

public class CorrelationIdHandler(IHttpContextAccessor accessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        var id = accessor.HttpContext?.Request.Headers[CorrelationIdMiddleware.HeaderName].ToString();
        if (!string.IsNullOrEmpty(id))
            request.Headers.TryAddWithoutValidation(CorrelationIdMiddleware.HeaderName, id);

        return base.SendAsync(request, ct);
    }
}