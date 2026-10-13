using System.Net;
using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Project.Application.Configuration;
using Project.Infrastructure;
using Project.Infrastructure.Configuration;
using Project.Infrastructure.Persistence;
using Project.Infrastructure.Tenancy;
using Project.Web.Http;
using Project.Web.Security;
using Scalar.AspNetCore;

namespace Project.Web;

/// <summary>Composition of the platform: one call in <c>Program.cs</c> for services, one for the pipeline.</summary>
public static class PlatformExtensions
{
    public const string AuthRateLimitPolicy = "auth";

    /// <summary>Infrastructure, authentication, authorization, ProblemDetails, CORS, rate limiting, OpenAPI, caching.</summary>
    public static IHostApplicationBuilder AddPlatform(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;

        // Runtime configuration from the platform database (everything except the bootstrap values).
        builder.Configuration.AddDatabaseConfiguration();
        services.AddInfrastructure(configuration);

        services.AddOptions<JwtOptions>().Bind(configuration.GetSection(JwtOptions.SectionName)).ValidateDataAnnotations();
        services.AddConfigurableSection<JwtOptions>(JwtOptions.SectionName, "identity", "Tokens de acceso: emisor, audiencia, duración y llave de firma.");
        services.AddConfigurableSection<CorsOptions>(CorsOptions.SectionName, "platform", "Orígenes permitidos cuando la SPA vive en otro dominio.");
        services.AddConfigurableSection<RateLimitingOptions>(RateLimitingOptions.SectionName, "platform", "Límites de peticiones por usuario o IP.");
        services.AddOptions<CorsOptions>().Bind(configuration.GetSection(CorsOptions.SectionName));
        services.AddOptions<RateLimitingOptions>().Bind(configuration.GetSection(RateLimitingOptions.SectionName)).ValidateDataAnnotations();
        services.AddOptions<HostingOptions>().Bind(configuration.GetSection(HostingOptions.SectionName));

        services.ConfigureHttpJsonOptions(o =>
        {
            o.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            o.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
        });

        services.AddProblemDetails(o => o.CustomizeProblemDetails = context =>
        {
            context.ProblemDetails.Instance ??= context.HttpContext.Request.Path;
            context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        });
        services.AddExceptionHandler<GlobalExceptionHandler>();

        AddAuthentication(services, configuration);
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddAuthorization();
        services.AddHostedService<EndpointSecurityValidator>();
        // First hosted service: in development the schema exists before Data Protection, the outbox or the jobs touch it.
        services.Insert(0, ServiceDescriptor.Singleton<IHostedService, MigrateOnStartupService>());
        services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

        services.AddCors();
        services.AddSingleton<Microsoft.AspNetCore.Cors.Infrastructure.ICorsPolicyProvider, ConfigurableCorsPolicyProvider>();

        AddRateLimiting(services, configuration);

        services.AddResponseCompression(o =>
        {
            o.EnableForHttps = true;
            o.Providers.Add<BrotliCompressionProvider>();
            o.Providers.Add<GzipCompressionProvider>();
        });
        services.AddOutputCache();
        services.AddOpenApi(o => o.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

        services.Configure<ForwardedHeadersOptions>(o =>
        {
            o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
            foreach (var proxy in configuration.GetSection("Hosting:KnownProxies").Get<string[]>() ?? [])
            {
                o.KnownProxies.Add(IPAddress.Parse(proxy));
            }

            foreach (var network in configuration.GetSection("Hosting:KnownNetworks").Get<string[]>() ?? [])
            {
                o.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
            }
        });

        return builder;
    }

    /// <summary>Middleware in the correct order. Call before mapping endpoints.</summary>
    public static WebApplication UsePlatform(this WebApplication app)
    {
        var hosting = app.Services.GetRequiredService<IOptions<HostingOptions>>().Value;

        app.UseForwardedHeaders();
        app.UseExceptionHandler();
        app.UseStatusCodePages();

        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
        }

        if (hosting.UseHttpsRedirection)
        {
            app.UseHttpsRedirection();
        }

        app.UseSecurityHeaders();
        app.UseResponseCompression();

        // Validation messages follow Accept-Language (es by default, en supported).
        app.UseRequestLocalization(o => o.SetDefaultCulture("es").AddSupportedCultures("es", "en").AddSupportedUICultures("es", "en"));

        if (hosting.ServeSpa)
        {
            app.UseDefaultFiles();
            app.UseStaticFiles();
        }

        app.UseRouting();
        app.UseCors();
        app.UseAuthentication();
        app.UseMiddleware<TenantResolutionMiddleware>();
        app.UseRateLimiter();
        app.UseAuthorization();
        app.UseOutputCache();

        if (hosting.EnableApiExplorer ?? app.Environment.IsDevelopment())
        {
            app.MapOpenApi().AllowAnonymous();
            app.MapScalarApiReference(o => o.WithTitle("Project API")).AllowAnonymous();
        }

        return app;
    }

    private static void AddAuthentication(IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        // Rebuilt whenever the Jwt section changes (database configuration), without a restart.
        services.AddSingleton<IOptionsChangeTokenSource<JwtBearerOptions>>(
            new ConfigurationChangeTokenSource<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, configuration.GetSection(JwtOptions.SectionName)));
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptionsMonitor<JwtOptions>>((bearer, monitor) =>
            {
                var jwt = monitor.CurrentValue;
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,

                    // Resolved per token: the key may be generated or rotated after startup.
                    IssuerSigningKeyResolver = (_, _, _, _) => [monitor.CurrentValue.GetSecurityKey()],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = "name",
                    RoleClaimType = "role",
                };
            });
    }

    private static void AddRateLimiting(IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        // Limits are read per request: a change in the configuration page applies at once (the limit is part of the
        // partition key, so new limits start new windows).
        static RateLimitingOptions Current(HttpContext context) =>
            context.RequestServices.GetRequiredService<IOptionsMonitor<RateLimitingOptions>>().CurrentValue;

        services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var settings = Current(context);
                return settings.Enabled
                    ? RateLimitPartition.GetFixedWindowLimiter($"{settings.GlobalPermitPerMinute}:{PartitionKey(context)}", _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = settings.GlobalPermitPerMinute,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    })
                    : RateLimitPartition.GetNoLimiter("disabled");
            });

            o.AddPolicy(AuthRateLimitPolicy, context =>
            {
                var settings = Current(context);
                return settings.Enabled
                    ? RateLimitPartition.GetFixedWindowLimiter($"auth:{settings.AuthPermitPer15Minutes}:{PartitionKey(context)}", _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = settings.AuthPermitPer15Minutes,
                        Window = TimeSpan.FromMinutes(15),
                        QueueLimit = 0,
                    })
                    : RateLimitPartition.GetNoLimiter("disabled");
            });
        });
    }

    private static string PartitionKey(HttpContext context)
    {
        var tenant = context.User.FindFirstValue(TenantClaims.TenantId) ?? context.Request.Host.Host;
        var caller = context.User.FindFirstValue("sub") ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return $"{tenant}:{caller}";
    }

    private static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;
                headers.XContentTypeOptions = "nosniff";
                headers.XFrameOptions = "DENY";
                headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                headers["Permissions-Policy"] = "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()";
                headers["Cross-Origin-Opener-Policy"] = "same-origin";

                // The API reference page loads its UI bundle from a CDN, so it keeps the default policy.
                if (!context.Request.Path.StartsWithSegments("/scalar"))
                {
                    headers.ContentSecurityPolicy =
                        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; " +
                        "font-src 'self' data:; connect-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";
                }

                return Task.CompletedTask;
            });

            await next();
        });

    /// <summary>Declares the Bearer scheme in the OpenAPI document.</summary>
    private sealed class BearerSecuritySchemeTransformer : Microsoft.AspNetCore.OpenApi.IOpenApiDocumentTransformer
    {
        public Task TransformAsync(OpenApiDocument document, Microsoft.AspNetCore.OpenApi.OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
        {
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
            };
            document.Security ??= [];
            document.Security.Add(new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference("Bearer", document)] = [] });
            return Task.CompletedTask;
        }
    }
}
