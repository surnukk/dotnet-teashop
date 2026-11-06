using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Orders.Api.HealthChecks;
using Orders.Api.HostedServices;
using Orders.Bll.Caching;
using Orders.Bll.Mappings;
using Orders.Bll.Services;
using Orders.Dal;
using Orders.Dal.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddRedisDistributedCache("redis");   // IDistributedCache поверх Redis; рядок підключення Aspire передає сам

builder.Services.AddMemoryCache(o =>
{
    o.SizeLimit = 5_000;
    o.TrackStatistics = true;
    o.CompactionPercentage = 0.2;
});
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

builder.Services.AddHostedService<CacheWarmupService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddProblemDetails(o => o.CustomizeProblemDetails = ctx =>
{
    ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;
});
builder.Services.AddExceptionHandler<Orders.Api.Infrastructure.DomainExceptionHandler>();


var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.MapDefaultEndpoints();         // /health і /alive (лише в Development)

if (app.Environment.IsDevelopment())
    app.Services.GetRequiredService<AutoMapper.IConfigurationProvider>().AssertConfigurationIsValid();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Logger.LogInformation("Database: {Database}",
    new SqlConnectionStringBuilder(connectionString).InitialCatalog);

app.MapControllers();
app.Run();