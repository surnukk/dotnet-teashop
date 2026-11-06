using Reviews.Application;
using Reviews.Application.Common.Caching;
using Reviews.Application.Common.Mappings;
using Reviews.Infrastructure;
using Reviews.WebApi.ExceptionHandling;
using Reviews.WebApi.HealthChecks;
using Reviews.WebApi.Mapping;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddMemoryCache(o =>
{
    o.SizeLimit = 5_000;
    o.TrackStatistics = true;
    o.CompactionPercentage = 0.2;
});
builder.Services.AddSingleton<ICacheService, MemoryCacheService>();
builder.Services.AddHealthChecks().AddCheck<CacheHealthCheck>("cache");


var aspireMongo = builder.Configuration.GetConnectionString("ReviewsDb");
if (!string.IsNullOrWhiteSpace(aspireMongo))
    builder.Configuration["MongoDb:ConnectionString"] = aspireMongo;

builder.Services.AddControllers();
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

builder.Services.AddHealthChecks().AddCheck<MongoHealthCheck>("mongodb");

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();

app.UseExceptionHandler();
app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();