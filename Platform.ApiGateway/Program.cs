using System.Diagnostics;
using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.Model;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();     // http://orders-api -> реальна адреса від Aspire

var app = builder.Build();

// 5.2: CorrelationId створюється тут, ДО MapReverseProxy
app.UseMiddleware<CorrelationIdMiddleware>();

// 5.3: логування маршрутизації й відповіді
app.Use(async (context, next) =>
{
    var sw = Stopwatch.StartNew();
    await next();
    sw.Stop();

    var proxy = context.Features.Get<IReverseProxyFeature>();
    var route = proxy?.Route.Config.RouteId ?? "none";
    var cluster = proxy?.Cluster?.Config.ClusterId ?? "none";

    app.Logger.LogInformation(
        "{Method} {Path} -> route {Route}, cluster {Cluster}: {StatusCode} in {Elapsed} ms, " +
        "response {ResponseSize} bytes, client {ClientIp}",
        context.Request.Method,
        context.Request.Path,
        route,
        cluster,
        context.Response.StatusCode,
        sw.ElapsedMilliseconds,
        context.Response.ContentLength ?? 0,
        context.Connection.RemoteIpAddress);

    // сервіс недоступний: timeout, connection refused тощо
    var error = context.Features.Get<IForwarderErrorFeature>();
    if (error is not null)
    {
        app.Logger.LogWarning(error.Exception,
            "Proxy error {ProxyError} for route {Route}: {Message}",
            error.Error, route, error.Exception?.Message);
    }
});

app.MapReverseProxy();
app.MapDefaultEndpoints();

app.Run();