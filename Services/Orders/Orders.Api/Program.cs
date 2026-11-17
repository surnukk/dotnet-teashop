using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Orders.Api.GrpcServices;
using Orders.Api.HealthChecks;
using Orders.Api.HostedServices;
using Orders.Api.IntegrationEvents;
using Orders.Api.Metrics;
using Orders.Bll.Caching;
using Orders.Bll.Mappings;
using Orders.Bll.Services;
using Orders.Dal;
using Orders.Dal.Interfaces;
using Orders.Dal.Migrations;
using Platform.EventBus;
using Platform.EventBus.Events;
using Platform.EventBus.Topology;
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

builder.AddRabbitMQClient("messaging");      
builder.Services.AddEventBusTopology();

builder.Services.AddEventHandler<ProductCreatedEvent, ProductCreatedHandler>();
builder.Services.AddEventHandler<ProductPriceChangedEvent, ProductPriceChangedHandler>();
builder.Services.AddEventHandler<ProductRenamedEvent, ProductRenamedHandler>();
builder.Services.AddEventHandler<ProductDeletedEvent, ProductDeletedHandler>();
builder.Services.AddHostedService<CatalogEventsConsumer>();
builder.Services.AddDlqDepthMonitor(EventBusTopology.OrdersCatalogSyncQueue);

builder.AddOrdersHealthChecks();

builder.AddRedisDistributedCache("redis", settings => settings.DisableHealthChecks = true);

builder.Services.AddMemoryCache(o =>
{
    o.SizeLimit = 5_000;
    o.TrackStatistics = true;
    o.CompactionPercentage = 0.2;
});

builder.Services.AddSingleton<OrderMetrics>();    
builder.Services.AddSingleton<ICacheMetrics, CacheMetrics>();
builder.Services.AddSingleton<ICacheService, TieredCacheService>();   // було MemoryCacheService
builder.Services.AddHealthChecks().AddCheck<CacheHealthCheck>("cache");

var connectionString = builder.Configuration.GetConnectionString("OrdersDb")
    ?? throw new InvalidOperationException("Connection string 'OrdersDb' is missing");

builder.Services.AddScoped<IUnitOfWork>(_ => new UnitOfWork(connectionString));

builder.Services.AddAutoMapper(cfg =>
{
    cfg.LicenseKey = builder.Configuration["AutoMapper:LicenseKey"];
    cfg.AddProfile<OrderProfile>();
});

builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IPromotionService, PromotionService>();
builder.Services.AddScoped<IOrderService, OrderService>();

builder.Services.AddControllers();

builder.Services.AddGrpc(o =>
{
    o.EnableDetailedErrors = builder.Environment.IsDevelopment();
    o.Interceptors.Add<LoggingInterceptor>();          // 6.2
});
builder.Services.AddGrpcReflection();

builder.Services.AddHostedService<CacheWarmupService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddProblemDetails(o => o.CustomizeProblemDetails = ctx =>
{
    ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;
});
builder.Services.AddExceptionHandler<Orders.Api.Infrastructure.DomainExceptionHandler>();


var app = builder.Build();

await DbMigrator.ApplyAsync(connectionString);

app.UseMiddleware<CorrelationIdMiddleware>();
app.MapDefaultEndpoints();         // /health і /alive (лише в Development)

if (app.Environment.IsDevelopment())
    app.Services.GetRequiredService<AutoMapper.IConfigurationProvider>().AssertConfigurationIsValid();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapGrpcReflectionService();
}

app.Logger.LogInformation("Database: {Database}",
    new SqlConnectionStringBuilder(connectionString).InitialCatalog);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGrpcService<OrdersGrpcService>();
app.MapGrpcHealthChecksService();       
app.Run();