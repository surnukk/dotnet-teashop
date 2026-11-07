using System.Text.Json.Serialization;
using Catalog.Contracts;
using Grpc.Core;
using Orders.Contracts;
using Reviews.Contracts;
using Platform.Aggregator.Services;
using Microsoft.Extensions.Http.Resilience;
using Platform.Aggregator.Caching;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddRedisDistributedCache("redis");   

builder.Services.ConfigureAll<HttpStandardResilienceOptions>(o =>
{
    o.Retry.MaxRetryAttempts = 1;                        
    o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(3);       
    o.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(8); 
});

// 6.3: camelCase за замовчуванням, null-значення не серіалізуються
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);

// 5.1: типізовані gRPC-клієнти; адреса за іменем сервісу, service discovery працює як для HttpClient
builder.Services.AddGrpcClient<OrdersService.OrdersServiceClient>(o => o.Address = new Uri("https://orders-api"));
builder.Services.AddGrpcClient<CatalogService.CatalogServiceClient>(o => o.Address = new Uri("https://catalog-api"));
builder.Services.AddGrpcClient<ReviewsService.ReviewsServiceClient>(o => o.Address = new Uri("https://reviews-api"));

builder.Services.AddScoped<OrderAggregator>();
builder.Services.AddSingleton<AggregateCache>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/api/aggregator/orders/{id:long}/full",
    async (long id, OrderAggregator aggregator, CancellationToken ct) =>
    {
        try
        {
            var result = await aggregator.GetFullOrderAsync(id, ct);
            return result is null
                ? Results.Problem(statusCode: 404, title: "Order not found", detail: $"Order {id} was not found")
                : Results.Ok(result);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.InvalidArgument)
        {
            return Results.Problem(statusCode: 400, title: "Invalid request", detail: ex.Status.Detail);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled && ct.IsCancellationRequested)
        {
            return Results.StatusCode(499);           
        }
        catch (Exception ex) when (OrderAggregator.IsServiceFailure(ex))
        {
            return Results.Problem(statusCode: 503, title: "orders-api is unavailable", detail: ex.Message);
        }
    })
    .WithTags("Aggregator");

app.MapDelete("/api/aggregator/orders/{id:long}/cache",
    async (long id, AggregateCache cache, CancellationToken ct) =>
    {
        await cache.RemoveAsync(AggregateCache.FullOrderKey(id), ct);
        return Results.NoContent();
    })
    .WithTags("Aggregator");

app.Run();