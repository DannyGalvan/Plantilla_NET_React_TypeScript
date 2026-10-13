using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Project.Application.Security;
using Project.Application.Settings;
using Project.Infrastructure.Tenancy;

namespace Project.Web.Security;

/// <summary>Endpoint metadata: the permission required to call the endpoint.</summary>
public sealed record PermissionMetadata(string Permission);

/// <summary>Endpoint metadata: any authenticated user may call it (profile, navigation, switch context).</summary>
public sealed record AuthenticatedOnlyMetadata;

/// <summary>Endpoint metadata: the endpoint is hidden (404) when the feature is disabled for the tenant.</summary>
public sealed record FeatureMetadata(string Feature);

public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}

/// <summary>Creates a policy per permission on demand (<c>perm:{key}</c>), so no policy has to be declared up front.</summary>
public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : DefaultAuthorizationPolicyProvider(options)
{
    public const string Prefix = "perm:";

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return await base.GetPolicyAsync(policyName);
        }

        return new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(policyName[Prefix.Length..]))
            .Build();
    }
}

/// <summary>Checks the requirement against the server-side permission service (cached, invalidated by version).</summary>
public sealed class PermissionAuthorizationHandler(IPermissionService permissions) : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var ct = (context.Resource as HttpContext)?.RequestAborted ?? CancellationToken.None;
        if (await permissions.HasPermissionAsync(requirement.Permission, ct))
        {
            context.Succeed(requirement);
        }
    }
}

public static class EndpointSecurityExtensions
{
    /// <summary>Requires an authenticated user with <paramref name="permission"/> (<c>{module}.{resource}.{action}</c>).</summary>
    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, string permission)
        where TBuilder : IEndpointConventionBuilder =>
        builder
            .WithMetadata(new PermissionMetadata(permission))
            .RequireAuthorization(PermissionPolicyProvider.Prefix + permission);

    /// <summary>Requires an authenticated user without a specific permission.</summary>
    public static TBuilder RequireAuthenticatedUser<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder =>
        builder.WithMetadata(new AuthenticatedOnlyMetadata()).RequireAuthorization();

    /// <summary>The endpoint can be called without a resolved tenant (global login, platform config).</summary>
    public static TBuilder AllowWithoutTenant<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder =>
        builder.WithMetadata(new AllowWithoutTenantAttribute());

    /// <summary>Returns 404 when <paramref name="feature"/> is disabled for the current tenant.</summary>
    public static TBuilder RequireFeature<TBuilder>(this TBuilder builder, string feature)
        where TBuilder : IEndpointConventionBuilder =>
        builder.WithMetadata(new FeatureMetadata(feature)).AddEndpointFilter(async (context, next) =>
        {
            var flags = context.HttpContext.RequestServices.GetService(typeof(IFeatureFlags)) as IFeatureFlags;
            return flags is not null && await flags.IsEnabledAsync(feature, context.HttpContext.RequestAborted)
                ? await next(context)
                : TypedResults.NotFound();
        });
}

/// <summary>
/// Fail-closed check at startup: every <c>/api</c> endpoint must declare <c>RequirePermission</c>,
/// <c>RequireAuthenticatedUser</c> or <c>AllowAnonymous</c>. Otherwise the application refuses to start.
/// </summary>
public sealed class EndpointSecurityValidator(
    EndpointDataSource endpoints,
    ILogger<EndpointSecurityValidator> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var offenders = Validate(endpoints.Endpoints);
        if (offenders.Count > 0)
        {
            throw new InvalidOperationException(
                "Endpoints without an explicit security declaration (RequirePermission, RequireAuthenticatedUser or AllowAnonymous): "
                + string.Join(", ", offenders));
        }

        logger.LogInformation("Endpoint security validated");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public static IReadOnlyList<string> Validate(IEnumerable<Endpoint> endpoints) =>
        endpoints.OfType<RouteEndpoint>()
            .Where(e => e.RoutePattern.RawText?.TrimStart('/').StartsWith("api/", StringComparison.OrdinalIgnoreCase) == true)
            .Where(e => e.Metadata.GetMetadata<PermissionMetadata>() is null
                && e.Metadata.GetMetadata<AuthenticatedOnlyMetadata>() is null
                && e.Metadata.GetMetadata<IAllowAnonymous>() is null)
            .Select(e => $"{string.Join('|', e.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? ["*"])} {e.RoutePattern.RawText}")
            .ToList();
}
