using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ServiceDiscovery;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Platform.ServiceDefaults.HealthChecks;

namespace Microsoft.Extensions.Hosting
{
    // Adds common Aspire services: service discovery, resilience, health checks, and OpenTelemetry.
    // This project should be referenced by each service project in your solution.
    // To learn more about using this project, see https://aka.ms/dotnet/aspire/service-defaults
    public static class Extensions
    {
        private const string HealthEndpointPath = "/health";
        private const string AlivenessEndpointPath = "/alive";

        public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder)
    where TBuilder : IHostApplicationBuilder
        {
            builder.Services.AddSerilog((services, cfg) => cfg
                .MinimumLevel.Is(builder.Environment.IsDevelopment() ? LogEventLevel.Debug : LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware", LogEventLevel.Fatal) 
                .Enrich.FromLogContext()
                .Enrich.WithProperty("ServiceName", builder.Environment.ApplicationName)
                .Enrich.WithEnvironmentName()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .WriteTo.Console(new CompactJsonFormatter()),
                writeToProviders: true);          // передає записи в OpenTelemetry → вкладка Structured

            builder.ConfigureOpenTelemetry();  
            builder.AddDefaultHealthChecks();

            builder.Services.AddServiceDiscovery();
            builder.Services.AddHttpContextAccessor();

            // 4.2 передавання CorrelationId у вихідні HTTP-запити
            builder.Services.AddTransient<CorrelationIdHandler>();

            builder.Services.ConfigureHttpClientDefaults(http =>
            {
                // 1.1: стандартний resilience-обробник (rate limiter → total timeout → retry → circuit breaker → attempt timeout)
                // підключається ОДИН раз для всіх HttpClient і gRPC-клієнтів; в інших місцях AddStandardResilienceHandler() не викликаємо,
                // щоб обробники не нашаровувалися. Для кастомного клієнта спершу RemoveAllResilienceHandlers(), потім власний.
                http.AddStandardResilienceHandler();
                http.AddServiceDiscovery();
                http.AddHttpMessageHandler<CorrelationIdHandler>();
            });

            return builder;
        }

        public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
        {
            builder.Logging.AddOpenTelemetry(logging =>
            {
                logging.IncludeFormattedMessage = true;
                logging.IncludeScopes = true;
            });

            builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddAttributes(new KeyValuePair<string, object>[]
            {
                new("deployment.environment", builder.Environment.EnvironmentName)
            }))
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                       .AddHttpClientInstrumentation()
                       .AddRuntimeInstrumentation()
                       .AddMeter("Yarp.ReverseProxy")
                       .AddMeter("Orders.Api")
                       .AddMeter("Catalog.Api")
                       .AddMeter("Reviews.Api")
                       .AddMeter("Catalog.Cache")
                       .AddMeter("Orders.Cache")
                       .AddMeter("Reviews.Cache")
                       .AddMeter("Grpc.Net.Client")
                       .AddMeter("Polly")
                       // межі кошиків для всіх гістограм тривалості (*.operation.duration), мс
                       .AddView("*.operation.duration", new ExplicitBucketHistogramConfiguration
                       {
                           Boundaries = new double[] { 5, 10, 25, 50, 100, 250, 500, 1000, 2500, 5000 }
                       });
            })
            .WithTracing(tracing =>
            {
                tracing
                      .SetSampler(builder.Environment.IsDevelopment()
                            ? new AlwaysOnSampler()
                            : new TraceIdRatioBasedSampler(builder.Configuration.GetValue("Telemetry:SamplingRatio", 0.1)))
                      .AddSource(builder.Environment.ApplicationName)
                      .AddSource("Platform.Messaging")
                      .AddSource("MongoDB.Driver.Core.Extensions.DiagnosticSources")      // MongoDB
                      .AddAspNetCoreInstrumentation()                                 
                      .AddHttpClientInstrumentation()
                      .AddSqlClientInstrumentation();                                     // Dapper і EF Core
            });

            builder.AddOpenTelemetryExporters();

            return builder;
        }

        private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
        {
            var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

            if (useOtlpExporter)
            {
                builder.Services.AddOpenTelemetry().UseOtlpExporter();
            }

            // Uncomment the following lines to enable the Azure Monitor exporter (requires the Azure.Monitor.OpenTelemetry.AspNetCore package)
            //if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
            //{
            //    builder.Services.AddOpenTelemetry()
            //       .UseAzureMonitor();
            //}

            return builder;
        }

        public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
        {
            builder.Services.AddSingleton<StartupHealthCheck>();

            builder.Services.AddHealthChecks()
                // liveness: лише чи відповідає процес, без зовнішніх залежностей (має бути швидким, < 100 мс)
                .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"])
                .AddCheck<StartupHealthCheck>("startup", tags: ["startup"]);

            return builder;
        }

        public static WebApplication MapDefaultEndpoints(this WebApplication app)
        {
            var showErrors = app.Environment.IsDevelopment();

            app.MapHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("live")
            });

            app.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("ready"),
                ResponseWriter = (context, report) => HealthJsonWriter.WriteAsync(context, report, showErrors)
            });

            app.MapHealthChecks("/health/startup", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("startup")
            });

            if (app.Environment.IsDevelopment())
            {
                app.MapHealthChecks("/health", new HealthCheckOptions
                {
                    ResponseWriter = (context, report) => HealthJsonWriter.WriteAsync(context, report, includeErrors: true)
                });
                app.MapHealthChecks("/alive", new HealthCheckOptions
                {
                    Predicate = r => r.Tags.Contains("live")
                });
            }

            return app;
        }

        private static Task WriteHealthResponse(HttpContext context, HealthReport report)
        {
            var payload = new
            {
                status = report.Status.ToString(),
                durationMs = (int)report.TotalDuration.TotalMilliseconds,
                checks = report.Entries.ToDictionary(
                    e => e.Key,
                    e => new { status = e.Value.Status.ToString(), description = e.Value.Description, data = e.Value.Data })
            };
            return context.Response.WriteAsJsonAsync(payload);
        }
    }
}
