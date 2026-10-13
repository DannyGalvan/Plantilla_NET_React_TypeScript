using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Project.Application.Organization;
using Project.Application.Tenancy;

namespace Project.Infrastructure.Tenancy;

/// <summary>One tenant resolution strategy. Strategies run in the order configured in <c>Tenancy:Resolvers</c>.</summary>
public interface ITenantResolver
{
    string Name { get; }

    ValueTask<TenantInfo?> ResolveAsync(HttpContext context, ITenantStore store, CancellationToken ct);
}

/// <summary>Custom domain registered for the tenant (<c>erp.acme.com</c>).</summary>
public sealed class HostTenantResolver : ITenantResolver
{
    public string Name => "Host";

    public ValueTask<TenantInfo?> ResolveAsync(HttpContext context, ITenantStore store, CancellationToken ct) =>
        store.FindByHostAsync(context.Request.Host.Host.ToLowerInvariant(), ct);
}

/// <summary>First label of <c>{code}.{RootDomain}</c>.</summary>
public sealed class SubdomainTenantResolver(IOptions<TenancyOptions> options) : ITenantResolver
{
    public string Name => "Subdomain";

    public ValueTask<TenantInfo?> ResolveAsync(HttpContext context, ITenantStore store, CancellationToken ct)
    {
        var root = options.Value.RootDomain;
        var host = context.Request.Host.Host.ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(root) || !host.EndsWith("." + root.ToLowerInvariant(), StringComparison.Ordinal))
        {
            return ValueTask.FromResult<TenantInfo?>(null);
        }

        var code = host[..^(root.Length + 1)];
        return code.Contains('.', StringComparison.Ordinal) ? ValueTask.FromResult<TenantInfo?>(null) : store.FindByCodeAsync(code, ct);
    }
}

/// <summary><c>tid</c> claim of the access token.</summary>
public sealed class ClaimTenantResolver : ITenantResolver
{
    public string Name => "Claim";

    public ValueTask<TenantInfo?> ResolveAsync(HttpContext context, ITenantStore store, CancellationToken ct) =>
        Guid.TryParse(context.User.FindFirstValue(TenantClaims.TenantId), out var id)
            ? store.FindByIdAsync(id, ct)
            : ValueTask.FromResult<TenantInfo?>(null);
}

/// <summary>
/// <c>X-Tenant</c> header with the tenant code. Disabled unless listed in <c>Tenancy:Resolvers</c>; meant for
/// trusted internal callers and the login screen of a SaaS without per-tenant domains.
/// </summary>
public sealed class HeaderTenantResolver(IOptions<TenancyOptions> options) : ITenantResolver
{
    public string Name => "Header";

    public ValueTask<TenantInfo?> ResolveAsync(HttpContext context, ITenantStore store, CancellationToken ct) =>
        context.Request.Headers.TryGetValue(options.Value.HeaderName, out var code) && !string.IsNullOrWhiteSpace(code)
            ? store.FindByCodeAsync(code.ToString().Trim().ToLowerInvariant(), ct)
            : ValueTask.FromResult<TenantInfo?>(null);
}

public static class TenantClaims
{
    public const string TenantId = "tid";
    public const string OrganizationUnitId = "ou";
    public const string PermissionsVersion = "pv";
}

/// <summary>Endpoint metadata: the endpoint works without a tenant (global login, platform config, health).</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class AllowWithoutTenantAttribute : Attribute;

/// <summary>
/// Resolves the tenant after authentication and before authorization:
/// <list type="number">
///   <item>Runs the configured resolvers (host, subdomain, header) and falls back to the <c>tid</c> claim.</item>
///   <item>Cross check: a token issued for tenant A used on the host of tenant B gets 403.</item>
///   <item>Suspended tenants get 403; unresolved tenants get 400 unless the endpoint allows it.</item>
///   <item>Prepares the dedicated connection and the organization scope.</item>
/// </list>
/// </summary>
public sealed class TenantResolutionMiddleware(RequestDelegate next, ILogger<TenantResolutionMiddleware> logger)
{
    public async Task InvokeAsync(
        HttpContext context,
        TenantContext tenantContext,
        IOptions<TenancyOptions> options,
        IOrganizationContext organization)
    {
        var ct = context.RequestAborted;
        if (tenantContext.Mode == TenancyMode.None)
        {
            await organization.InitializeAsync(ct);
            await next(context);
            return;
        }

        var services = context.RequestServices;
        var store = services.GetRequiredService<ITenantStore>();
        var resolvers = services.GetServices<ITenantResolver>().ToDictionary(r => r.Name, StringComparer.OrdinalIgnoreCase);
        var allowWithout = context.GetEndpoint()?.Metadata.GetMetadata<AllowWithoutTenantAttribute>() is not null;

        TenantInfo? resolved = null;
        foreach (var name in options.Value.Resolvers.Where(n => !n.Equals("Claim", StringComparison.OrdinalIgnoreCase)))
        {
            if (resolvers.TryGetValue(name, out var resolver) && (resolved = await resolver.ResolveAsync(context, store, ct)) is not null)
            {
                break;
            }
        }

        if (context.User.Identity?.IsAuthenticated == true && Guid.TryParse(context.User.FindFirstValue(TenantClaims.TenantId), out var tokenTenant))
        {
            if (resolved is not null && resolved.Id != tokenTenant)
            {
                logger.LogWarning("Token of tenant {TokenTenant} used on host of tenant {HostTenant}", tokenTenant, resolved.Id);
                await WriteProblemAsync(context, StatusCodes.Status403Forbidden, "tenant.mismatch", "The token does not belong to this tenant.");
                return;
            }

            resolved ??= await store.FindByIdAsync(tokenTenant, ct);
        }

        if (resolved is null)
        {
            if (allowWithout)
            {
                await next(context);
                return;
            }

            await WriteProblemAsync(context, StatusCodes.Status400BadRequest, "tenant.not_resolved", "The tenant could not be resolved.");
            return;
        }

        if (resolved.Status != TenantStatus.Active && !allowWithout)
        {
            await WriteProblemAsync(context, StatusCodes.Status403Forbidden, "tenant.suspended", "The tenant is not active.");
            return;
        }

        tenantContext.Set(resolved);
        await services.GetRequiredService<ITenantConnectionResolver>().PrepareAsync(resolved, ct);
        await organization.InitializeAsync(ct);

        using (logger.BeginScope(new Dictionary<string, object> { ["TenantId"] = resolved.Id, ["Tenant"] = resolved.Code }))
        {
            await next(context);
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, int status, string code, string detail)
    {
        var problem = new ProblemDetails { Status = status, Title = code, Detail = detail, Instance = context.Request.Path };
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = context.TraceIdentifier;
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(problem, (System.Text.Json.JsonSerializerOptions?)null, "application/problem+json");
    }
}

/// <summary>Runs work for every active tenant with bounded parallelism, one scope per tenant.</summary>
public sealed class TenantJobRunner(
    IServiceScopeFactory scopeFactory,
    IOptions<TenancyOptions> tenancy,
    ILogger<TenantJobRunner> logger) : ITenantJobRunner
{
    public async Task ForEachTenantAsync(Func<IServiceProvider, TenantInfo, CancellationToken, Task> work, CancellationToken ct)
    {
        if (tenancy.Value.Mode == TenancyMode.None)
        {
            await using var single = scopeFactory.CreateAsyncScope();
            await single.ServiceProvider.GetRequiredService<IOrganizationContext>().InitializeAsync(ct);
            await work(single.ServiceProvider, TenantContext.DefaultTenant, ct);
            return;
        }

        IReadOnlyList<TenantInfo> tenants;
        await using (var listing = scopeFactory.CreateAsyncScope())
        {
            tenants = await listing.ServiceProvider.GetRequiredService<ITenantStore>().ListActiveAsync(ct);
        }

        await Parallel.ForEachAsync(tenants, new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = ct }, async (tenant, token) =>
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            try
            {
                scope.ServiceProvider.GetRequiredService<ITenantSetter>().Set(tenant);
                await scope.ServiceProvider.GetRequiredService<ITenantConnectionResolver>().PrepareAsync(tenant, token);
                await scope.ServiceProvider.GetRequiredService<IOrganizationContext>().InitializeAsync(token);
                await work(scope.ServiceProvider, tenant, token);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Tenant job failed for tenant {Tenant}", tenant.Code);
            }
        });
    }
}
