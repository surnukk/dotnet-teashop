using Catalog.Contracts;
using Grpc.Core;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Orders.Contracts;
using Platform.Aggregator.Caching;
using Platform.Aggregator.Resilience;
using Platform.Aggregator.Services;
using Reviews.Contracts;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddRedisDistributedCache("redis");

builder.Services.AddSingleton<IConfigureOptions<HttpStandardResilienceOptions>, AggregatorResilienceSetup>();

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);

builder.Services.AddGrpcClient<OrdersService.OrdersServiceClient>(o => o.Address = new Uri("https://orders-api"))
    .ConfigureChannel(c => c.ServiceConfig = GrpcRetryPolicy.Create());

builder.Services.AddGrpcClient<CatalogService.CatalogServiceClient>(o => o.Address = new Uri("https://catalog-api"))
    .ConfigureChannel(c => c.ServiceConfig = GrpcRetryPolicy.Create(maxAttempts: 4))
    .RemoveAllResilienceHandlers()      
    .AddStandardResilienceHandler(ResiliencePolicies.Critical);   

builder.Services.AddGrpcClient<ReviewsService.ReviewsServiceClient>(o => o.Address = new Uri("https://reviews-api"))
    .RemoveAllResilienceHandlers()
    .AddStandardResilienceHandler(ResiliencePolicies.NonCritical);

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