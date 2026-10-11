using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Instrumentation.Http;
using OpenTelemetry.Instrumentation.EntityFrameworkCore;
using OpenTelemetry.Instrumentation.Runtime;
using Serilog;
using Serilog.Sinks.OpenTelemetry;

namespace Project.ServiceDefaults;

/// <summary>
/// Aspire-style shared bootstrap. Wires logging, telemetry, health, cache and
/// standard resilience handlers for outbound HttpClients. Consumed by
/// <c>Project.Api</c> via <c>builder.AddServiceDefaults()</c>.
/// </summary>
public static class ServiceDefaultsExtensions
{
    public static WebApplicationBuilder AddServiceDefaults(this WebApplicationBuilder builder)
    {
        builder.AddSerilogDefaults();
        builder.AddOpenTelemetryDefaults();
        builder.AddDefaultHealthChecks();
        builder.AddDefaultCaching();
        builder.AddDefaultResilience();
        return builder;
    }

    private static WebApplicationBuilder AddSerilogDefaults(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, services, config) =>
        {
            config
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("service.name", context.HostingEnvironment.ApplicationName)
                .WriteTo.Console(new Serilog.Formatting.Compact.CompactJsonFormatter())
                .WriteTo.OpenTelemetry(opts =>
                {
                    opts.Endpoint = context.Configuration["Otel:Endpoint"] ?? "http://localhost:4317";
                    opts.Protocol = OtlpProtocol.Grpc;
                    opts.ResourceAttributes = new Dictionary<string, object>
                    {
                        ["service.name"] = context.HostingEnvironment.ApplicationName,
                    };
                });
        });

        return builder;
    }

    private static WebApplicationBuilder AddOpenTelemetryDefaults(this WebApplicationBuilder builder)
    {
        var serviceName = builder.Environment.ApplicationName;

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddOtlpExporter(opts =>
                    {
                        opts.Endpoint = new Uri(builder.Configuration["Otel:Endpoint"] ?? "http://localhost:4317");
                    });
            });

        return builder;
    }

    private static WebApplicationBuilder AddDefaultHealthChecks(this WebApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks();
        return builder;
    }

    private static WebApplicationBuilder AddDefaultCaching(this WebApplicationBuilder builder)
    {
        var redis = builder.Configuration["Cache:Redis:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(redis))
        {
            builder.Services.AddStackExchangeRedisCache(opts => opts.Configuration = redis);
        }

        builder.Services.AddHybridCache();
        return builder;
    }

    private static WebApplicationBuilder AddDefaultResilience(this WebApplicationBuilder builder)
    {
        builder.Services.AddHttpClient();
        return builder;
    }
}

public static class WebApplicationDefaultsExtensions
{
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health/live");
        app.MapHealthChecks("/health/ready");
        return app;
    }
}