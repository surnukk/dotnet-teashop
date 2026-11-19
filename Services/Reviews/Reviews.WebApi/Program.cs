using Platform.EventBus;
using Platform.EventBus.Events;
using Platform.EventBus.Topology;
using Reviews.Application;
using Reviews.Application.Common.Caching;
using Reviews.Application.Common.Mappings;
using Reviews.Infrastructure;
using Reviews.WebApi.ExceptionHandling;
using Reviews.WebApi.Grpc;
using Reviews.WebApi.GrpcServices;
using Reviews.WebApi.HealthChecks;
using Reviews.WebApi.IntegrationEvents;
using Reviews.WebApi.Mapping;
using Reviews.WebApi.Metrics;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Platform.ServiceDefaults.Security;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

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
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>   // Auth Service (HS256), як було
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
    .AddJwtBearer(PlatformAuthentication.IdentityServerScheme, options =>   // IdentityServer
    {
        options.Authority = identityServerAuthority;
        options.Audience = "reviews_api";
        options.RequireHttpsMetadata = false;
        options.MapInboundClaims = false;
        options.RefreshInterval = TimeSpan.FromSeconds(10);
        options.AutomaticRefreshInterval = TimeSpan.FromMinutes(5);
        options.TokenValidationParameters.NameClaimType = "name";
        options.TokenValidationParameters.RoleClaimType = "role";
    });

builder.Services.AddAuthorization(options =>
{
    options.AddScopePolicy("reviews.read", "reviews.read", jwt["Issuer"]);
});

builder.AddRabbitMQClient("messaging");
builder.Services.AddEventBusTopology();

builder.Services.AddEventHandler<ProductRenamedEvent, ProductRenamedHandler>();
builder.Services.AddHostedService<CatalogEventsConsumer>();
builder.Services.AddDlqDepthMonitor(EventBusTopology.ReviewsCatalogSyncQueue);

builder.AddReviewsHealthChecks();

builder.AddRedisDistributedCache("redis", settings => settings.DisableHealthChecks = true);   // IDistributedCache поверх Redis; рядок підключення Aspire передає сам

builder.Services.AddMemoryCache(o =>
{
    o.SizeLimit = 5_000;
    o.TrackStatistics = true;
    o.CompactionPercentage = 0.2;
});

builder.Services.AddSingleton<ReviewMetrics>();
builder.Services.AddSingleton<ICacheMetrics, CacheMetrics>();
builder.Services.AddSingleton<ICacheService, TieredCacheService>();   // було MemoryCacheService
builder.Services.AddHealthChecks().AddCheck<CacheHealthCheck>("cache");


var aspireMongo = builder.Configuration.GetConnectionString("ReviewsDb");
if (!string.IsNullOrWhiteSpace(aspireMongo))
    builder.Configuration["MongoDb:ConnectionString"] = aspireMongo;

builder.Services.AddControllers();

builder.Services.AddSingleton<FaultInjection>();
builder.Services.AddGrpc(o =>
{
    o.EnableDetailedErrors = builder.Environment.IsDevelopment();
    o.Interceptors.Add<LoggingInterceptor>();
    if (builder.Environment.IsDevelopment())
        o.Interceptors.Add<FaultInjectionInterceptor>();
});
builder.Services.AddGrpcReflection();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services
    .AddApplication(builder.Configuration)       // MediatR, behaviors, валідатори
    .AddInfrastructure(builder.Configuration);   // MongoDB

builder.Services.AddAutoMapper(cfg =>
{
    cfg.LicenseKey = builder.Configuration["LuckyPenny:LicenseKey"];
    cfg.AddMaps(typeof(ReviewProfile), typeof(ReviewRequestProfile));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapGet("/api/test/delay/{ms:int}", (int ms, FaultInjection faults) =>
    {
        faults.DelayMs = Math.Clamp(ms, 0, 30_000);
        return Results.Ok(new { delayMs = faults.DelayMs });
    });
}

app.UseMiddleware<CorrelationIdMiddleware>();

app.UseExceptionHandler();
app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapGrpcReflectionService();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGrpcService<ReviewsGrpcService>();
app.MapGrpcHealthChecksService();
app.Run();