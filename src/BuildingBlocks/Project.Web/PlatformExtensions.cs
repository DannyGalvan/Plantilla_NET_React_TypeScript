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
using Project.Infrastructure;
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

        services.AddInfrastructure(configuration);

        services.AddOptions<JwtOptions>().Bind(configuration.GetSection(JwtOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
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

        services.AddCors(o => o.AddDefaultPolicy(policy =>
        {
            var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
            if (origins.Length > 0)
            {
                policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
            }
        }));

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
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwt) =>
            {
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Value.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Value.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = jwt.Value.GetSecurityKey(),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = "name",
                    RoleClaimType = "role",
                };
            });
    }

    private static void AddRateLimiting(IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        var settings = configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>() ?? new RateLimitingOptions();

        services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Policies always exist so endpoints can reference them; when disabled they never limit.
            if (!settings.Enabled)
            {
                o.AddPolicy(AuthRateLimitPolicy, _ => RateLimitPartition.GetNoLimiter("disabled"));
                return;
            }

            o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(PartitionKey(context), _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = settings.GlobalPermitPerMinute,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                }));

            o.AddPolicy(AuthRateLimitPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter($"auth:{PartitionKey(context)}", _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = settings.AuthPermitPer15Minutes,
                    Window = TimeSpan.FromMinutes(15),
                    QueueLimit = 0,
                }));
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
