using System.Text.Json.Serialization;
using Platform.Aggregator.Clients;
using Platform.Aggregator.Services;
using Microsoft.Extensions.Http.Resilience;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.ConfigureAll<HttpStandardResilienceOptions>(o =>
{
    o.Retry.MaxRetryAttempts = 1;                        
    o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(3);       
    o.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(8); 
});

// 6.3: camelCase за замовчуванням, null-значення не серіалізуються
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);

// 6.1: типізовані клієнти; адреса за іменем сервісу, схема «https, якщо доступний, інакше http»
builder.Services.AddHttpClient<OrdersClient>(c => c.BaseAddress = new("https+http://orders-api"));
builder.Services.AddHttpClient<CatalogClient>(c => c.BaseAddress = new("https+http://catalog-api"));
builder.Services.AddHttpClient<ReviewsClient>(c => c.BaseAddress = new("https+http://reviews-api"));

builder.Services.AddScoped<OrderAggregator>();

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

// 6.2: GET /api/aggregator/orders/{id}/full
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
        catch (Exception ex) when (ex is HttpRequestException
                              or Polly.Timeout.TimeoutRejectedException
                              or Polly.CircuitBreaker.BrokenCircuitException)
        {
            // недоступний саме orders-api: без замовлення відповісти нічого
            return Results.Problem(statusCode: 503, title: "orders-api is unavailable", detail: ex.Message);
        }
    })
    .WithTags("Aggregator");

app.Run();