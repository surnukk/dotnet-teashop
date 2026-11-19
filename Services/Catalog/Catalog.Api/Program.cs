using Catalog.Api.ExceptionHandling;
using Catalog.Api.Grpc;
using Catalog.Api.GrpcServices;
using Catalog.Api.HealthChecks;
using Catalog.Api.HostedServices;
using Catalog.Bll.Caching;
using Catalog.Bll.Mapping;
using Catalog.Bll.Services;
using Catalog.Bll.Validators;
using Catalog.Dal;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Catalog.Api.Metrics;
using Platform.EventBus;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Platform.ServiceDefaults.Security;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();      // логування, трасування, health checks, service discovery

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
        options.Audience = "products_api";             
        options.RequireHttpsMetadata = false;           
        options.MapInboundClaims = false;
        options.RefreshInterval = TimeSpan.FromSeconds(10);     
        options.AutomaticRefreshInterval = TimeSpan.FromMinutes(5);
        options.TokenValidationParameters.NameClaimType = "name";
        options.TokenValidationParameters.RoleClaimType = "role";
    });

// ---------- Scope-based політики ----------
var authServiceIssuer = jwt["Issuer"];
builder.Services.AddAuthorization(options =>
{
    options.AddScopePolicy("products.read", "products.read", authServiceIssuer);
    options.AddScopePolicy("products.write", "products.write", authServiceIssuer, firstPartyRole: "Admin");
});

builder.AddRabbitMQClient("messaging");        // реєструє IConnection (singleton) + телеметрію
builder.Services.AddEventBusTopology();
builder.Services.AddEventPublisher();

builder.AddCatalogHealthChecks();

builder.AddRedisDistributedCache("redis", settings => settings.DisableHealthChecks = true);   // IDistributedCache поверх Redis; рядок підключення Aspire передає сам

builder.Services.AddMemoryCache(o =>
{
    o.SizeLimit = 10_000;  
    o.TrackStatistics = true;  
    o.CompactionPercentage = 0.2; 
});

builder.Services.AddSingleton<CatalogMetrics>();
builder.Services.AddSingleton<ICacheMetrics, CacheMetrics>();
builder.Services.AddSingleton<ICacheService, TieredCacheService>();   // було MemoryCacheService
builder.Services.AddHealthChecks().AddCheck<CacheHealthCheck>("cache");

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("CatalogDb")));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ITagService, TagService>();

builder.Services.AddAutoMapper(cfg =>
{
    cfg.LicenseKey = builder.Configuration["AutoMapper:LicenseKey"];
    cfg.AddProfile<MappingProfile>();
});

builder.Services.AddValidatorsFromAssemblyContaining<CreateProductDtoValidator>();

builder.Services.AddHostedService<CacheWarmupService>();

// Add services to the container.

builder.Services.AddControllers(o => o.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true);


builder.Services.AddGrpc(o =>
{
    o.EnableDetailedErrors = builder.Environment.IsDevelopment();
    o.Interceptors.Add<LoggingInterceptor>();    
});
builder.Services.AddGrpcReflection();   // потрібен лише для перевірки в кроках 9–10

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

ValidatorOptions.Global.PropertyNameResolver = (_, member, _) =>
    member is null ? null : char.ToLowerInvariant(member.Name[0]) + member.Name[1..];


builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

app.MapDefaultEndpoints();         // /health і /alive (лише в Development)

app.UseMiddleware<CorrelationIdMiddleware>();

app.UseExceptionHandler();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    if (!app.Environment.IsProduction())
        await DbInitializer.SeedAsync(db);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapGrpcReflectionService();
}

//app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGrpcService<CatalogGrpcService>();
app.MapGrpcHealthChecksService();

app.Run();
