using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Formatting.Compact;

namespace Project.ServiceDefaults;

/// <summary>
/// Shared bootstrap for every host (API, migration service): structured logging, telemetry, health and resilience.
/// OpenTelemetry exports only when <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> (or <c>Observability:OtlpEndpoint</c>) is set.
/// </summary>
public static class ServiceDefaultsExtensions
{
    public const string ReadyTag = "ready";

    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        var otlp = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? builder.Configuration["Observability:OtlpEndpoint"];
        var serviceName = builder.Environment.ApplicationName;

        builder.Services.AddSerilog((services, logger) =>
        {
            logger
                .ReadFrom.Configuration(builder.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("service", serviceName);

            if (builder.Environment.IsDevelopment())
            {
                logger.WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}");
            }
            else
            {
                logger.WriteTo.Console(new RenderedCompactJsonFormatter());
            }

            if (!string.IsNullOrWhiteSpace(otlp))
            {
                logger.WriteTo.OpenTelemetry(o =>
                {
                    o.Endpoint = otlp;
                    o.ResourceAttributes = new Dictionary<string, object> { ["service.name"] = serviceName };
                });
            }
        },
        // Each host keeps its own logger instead of replacing the static Log.Logger (several hosts can share a process, e.g. in tests).
        preserveStaticLogger: true);

        var telemetry = builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(serviceName))
            .WithTracing(tracing => tracing
                .AddSource(serviceName)
                .AddAspNetCoreInstrumentation(o => o.Filter = context => !context.Request.Path.StartsWithSegments("/health"))
                .AddHttpClientInstrumentation())
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation());

        if (!string.IsNullOrWhiteSpace(otlp))
        {
            telemetry.UseOtlpExporter(OpenTelemetry.Exporter.OtlpExportProtocol.Grpc, new Uri(otlp));
        }

        builder.Services.AddHealthChecks().AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);
        // Retries only for idempotent methods: a retried POST/PUT duplicates uploads, payments or webhook deliveries
        // (and a streamed body cannot be sent twice). Callers that need retries on writes handle them explicitly.
        builder.Services.ConfigureHttpClientDefaults(http => http.AddStandardResilienceHandler(o => o.Retry.DisableForUnsafeHttpMethods()));
        return builder;
    }

    /// <summary>
    /// <c>/health/live</c>: the process answers. <c>/health/ready</c>: dependencies (database) are reachable.
    /// </summary>
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = check => check.Tags.Contains("live") })
            .AllowAnonymous()
            .DisableHttpMetrics();
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains(ReadyTag),
                ResponseWriter = WriteJsonAsync,
            })
            .AllowAnonymous()
            .DisableHttpMetrics();
        return app;
    }

    private static Task WriteJsonAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsJsonAsync(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new { name = e.Key, status = e.Value.Status.ToString(), duration = e.Value.Duration.TotalMilliseconds }),
        });
    }
}
