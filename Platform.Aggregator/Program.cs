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
using Platform.Aggregator.HealthChecks;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Platform.ServiceDefaults.Security;
using Platform.Aggregator.Security;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// ---------- Authentication: Auth Service (HS256, Ч.1) + Duende IdentityServer (OAuth 2.0, Ч.2) ----------
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
    // токени Auth Service — без змін із Частини 1
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
    // токени IdentityServer: підпис береться з /.well-known/openid-configuration
    .AddJwtBearer(PlatformAuthentication.IdentityServerScheme, options =>
    {
        options.Authority = identityServerAuthority;
        options.Audience = "orders_api";         
        options.RequireHttpsMetadata = false;           
        options.MapInboundClaims = false;
        options.TokenValidationParameters.NameClaimType = "name";
        options.TokenValidationParameters.RoleClaimType = "role";
    });

// ---------- Scope-based політики ----------
var authServiceIssuer = jwt["Issuer"];
builder.Services.AddAuthorization(options =>
{
    options.AddScopePolicy("orders.read", "orders.read", authServiceIssuer);
    options.AddScopePolicy("orders.manage", "orders.write", authServiceIssuer, firstPartyRole: "Admin");
});

builder.Services.AddHttpClient(ClientCredentialsTokenProvider.HttpClientName);
builder.Services.AddSingleton<IClientTokenProvider, ClientCredentialsTokenProvider>();

builder.AddAggregatorHealthChecks();

builder.AddRedisDistributedCache("redis", settings => settings.DisableHealthChecks = true);

builder.Services.AddSingleton<IConfigureOptions<HttpStandardResilienceOptions>, AggregatorResilienceSetup>();

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);

builder.Services.AddGrpcClient<OrdersService.OrdersServiceClient>(o => o.Address = new Uri("https://orders-api"))
    .ConfigureChannel(c => c.ServiceConfig = GrpcRetryPolicy.Create()).AddClientCredentialsToken();

builder.Services.AddGrpcClient<CatalogService.CatalogServiceClient>(o => o.Address = new Uri("https://catalog-api"))
    .ConfigureChannel(c => c.ServiceConfig = GrpcRetryPolicy.Create(maxAttempts: 4))
    .AddClientCredentialsToken()
    .RemoveAllResilienceHandlers()      
    .AddStandardResilienceHandler(ResiliencePolicies.Critical);   

builder.Services.AddGrpcClient<ReviewsService.ReviewsServiceClient>(o => o.Address = new Uri("https://reviews-api"))
    .AddClientCredentialsToken()
    .RemoveAllResilienceHandlers()
    .AddStandardResilienceHandler(ResiliencePolicies.NonCritical);

builder.Services.AddScoped<OrderAggregator>();
builder.Services.AddSingleton<AggregateCache>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.MapDefaultEndpoints();

app.UseAuthentication();
app.UseAuthorization();

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
    .WithTags("Aggregator").RequireAuthorization("orders.read");

app.MapDelete("/api/aggregator/orders/{id:long}/cache",
    async (long id, AggregateCache cache, CancellationToken ct) =>
    {
        await cache.RemoveAsync(AggregateCache.FullOrderKey(id), ct);
        return Results.NoContent();
    })
    .WithTags("Aggregator").RequireAuthorization("orders.manage");

app.Run();