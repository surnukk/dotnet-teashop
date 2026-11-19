using System.Diagnostics;
using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.Model;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Platform.ServiceDefaults.Security;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// ---------- Auth: токени Auth Service + токени IdentityServer; Gateway валідує ПЕРЕД проксіюванням ----------
var jwt = builder.Configuration.GetSection("Jwt");
var jwtSecret = jwt["SecretKey"] ?? throw new InvalidOperationException("Jwt:SecretKey is missing");
var identityServerAuthority = builder.Configuration["IdentityServer:Authority"] ?? "http://localhost:5200";

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = PlatformAuthentication.SmartScheme;
        options.DefaultChallengeScheme = PlatformAuthentication.SmartScheme;
    })
    .AddPolicyScheme(PlatformAuthentication.SmartScheme, "Auth Service or IdentityServer", options =>
    {
        options.ForwardDefaultSelector = context =>
            PlatformAuthentication.SelectScheme(context, identityServerAuthority);
    })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
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
    })
    .AddJwtBearer(PlatformAuthentication.IdentityServerScheme, options =>
    {
        options.Authority = identityServerAuthority;
        options.RequireHttpsMetadata = false;
        options.MapInboundClaims = false;
        options.RefreshInterval = TimeSpan.FromSeconds(10);          // NEW: дозволяє оновити метадані вже через 10 с після збою
        options.AutomaticRefreshInterval = TimeSpan.FromMinutes(5);
        // Gateway не ресурс-сервер: приймає токени для будь-якого з ресурсів, а audience окремого сервісу перевіряє він сам
        options.TokenValidationParameters.ValidAudiences = new[] { "orders_api", "products_api", "payments_api" };
        options.TokenValidationParameters.NameClaimType = "name";
        options.TokenValidationParameters.RoleClaimType = "role";
    });
builder.Services.AddAuthorization();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver()
    .AddTransforms(transformContext =>
    {
        transformContext.AddRequestTransform(context =>
        {
            context.ProxyRequest.Headers.Remove("X-User-Id");       // клієнт не може підробити заголовок
            var userId = context.HttpContext.User.FindFirst("sub")?.Value;
            if (!string.IsNullOrEmpty(userId))                      // у токена Client Credentials sub немає
                context.ProxyRequest.Headers.TryAddWithoutValidation("X-User-Id", userId);
            return ValueTask.CompletedTask;
        });
    });

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