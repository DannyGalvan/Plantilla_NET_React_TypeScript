using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Project.Web.Security;

/// <summary>
/// Default CORS policy built from the current <c>Cors:AllowedOrigins</c>, so origins edited in the configuration page
/// apply without a restart. No origins means no cross-origin access (the SPA is served by the API).
/// </summary>
public sealed class ConfigurableCorsPolicyProvider(IOptionsMonitor<CorsOptions> options) : ICorsPolicyProvider
{
    private static readonly CorsPolicy Closed = new();

    public Task<CorsPolicy?> GetPolicyAsync(HttpContext context, string? policyName)
    {
        var origins = options.CurrentValue.AllowedOrigins.Where(o => !string.IsNullOrWhiteSpace(o)).ToArray();
        if (origins.Length == 0)
        {
            return Task.FromResult<CorsPolicy?>(Closed);
        }

        var policy = new CorsPolicyBuilder().WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials().Build();
        return Task.FromResult<CorsPolicy?>(policy);
    }
}
