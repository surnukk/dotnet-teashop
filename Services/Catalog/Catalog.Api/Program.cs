using Catalog.Api.ExceptionHandling;
using Catalog.Api.HealthChecks;
using Catalog.Api.HostedServices;
using Catalog.Bll.Caching;
using Catalog.Bll.Mapping;
using Catalog.Bll.Services;
using Catalog.Bll.Validators;
using Catalog.Dal;
using FluentValidation;
using Catalog.Api.GrpcServices;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();      // логування, трасування, health checks, service discovery

builder.AddRedisDistributedCache("redis");   // IDistributedCache поверх Redis; рядок підключення Aspire передає сам

builder.Services.AddMemoryCache(o =>
{
    o.SizeLimit = 10_000;  
    o.TrackStatistics = true;  
    o.CompactionPercentage = 0.2; 
});
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


builder.Services.AddGrpc(o => o.EnableDetailedErrors = builder.Environment.IsDevelopment());
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

app.UseAuthorization();

app.MapControllers();

app.MapGrpcService<CatalogGrpcService>();

app.Run();
