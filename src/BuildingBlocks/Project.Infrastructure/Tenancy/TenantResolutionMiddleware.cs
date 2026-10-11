using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Project.Infrastructure.Tenancy;

/// <summary>
/// Runs the configured <see cref="ITenantResolver"/> chain BEFORE authentication,
/// rate limit and output cache. Endpoints marked <c>.AllowWithoutTenant()</c>
/// bypass the resolution (login global, /platform/config, /health/*).
/// </summary>
public sealed class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ITenantContext tenantContext,
        ITenantSetter setter,
        IEnumerable<ITenantResolver> resolvers)
    {
        if (tenantContext.IsResolved)
        {
            await _next(context);
            return;
        }

        if (context.GetEndpoint()?.Metadata.GetMetadata<AllowWithoutTenantAttribute>() is not null)
        {
            await _next(context);
            return;
        }

        foreach (var resolver in resolvers.OrderBy(r => r.Order))
        {
            var code = await resolver.ResolveCodeAsync(context, context.RequestAborted);
            if (string.IsNullOrWhiteSpace(code)) continue;

            // Tenant lookup happens here (cached via ITenantStore).
            // Phase 2 wires EfTenantStore when the Tenancy module lands (Fase 4B).
            // For now the middleware only records the resolved code in the log scope.
            context.Items["tenant:resolved-code"] = code;
            break;
        }

        await _next(context);
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class AllowWithoutTenantAttribute : Attribute;