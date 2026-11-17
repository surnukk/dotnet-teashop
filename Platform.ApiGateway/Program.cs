using System.Diagnostics;
using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.Model;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var jwt = builder.Configuration.GetSection("Jwt");
var jwtSecret = jwt["SecretKey"] ?? throw new InvalidOperationException("Jwt:SecretKey is missing");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwt["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            NameClaimType = "unique_name",
            RoleClaimType = "role"
        };
    });
builder.Services.AddAuthorization();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapReverseProxy();
app.MapDefaultEndpoints();

app.Run();