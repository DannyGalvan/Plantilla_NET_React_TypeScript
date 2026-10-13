using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Project.Web.Security;

/// <summary>
/// <c>Jwt</c> section. <see cref="SigningKey"/> must come from user-secrets or an environment variable
/// (<c>Jwt__SigningKey</c>); generate one with <c>openssl rand -base64 48</c>.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; set; } = "project-api";

    [Required]
    public string Audience { get; set; } = "project-client";

    [Required]
    [MinLength(32, ErrorMessage = "Jwt:SigningKey must have at least 32 characters.")]
    public string SigningKey { get; set; } = string.Empty;

    [Range(1, 1440)]
    public int AccessTokenMinutes { get; set; } = 15;

    [Range(1, 365)]
    public int RefreshTokenDays { get; set; } = 7;

    public SymmetricSecurityKey GetSecurityKey() => new(Encoding.UTF8.GetBytes(SigningKey));
}

/// <summary><c>Cors</c> section. Only needed when the SPA is served from another origin.</summary>
public sealed class CorsOptions
{
    public const string SectionName = "Cors";

    public string[] AllowedOrigins { get; set; } = [];
}

/// <summary><c>RateLimiting</c> section. Limits are per tenant and user (or IP for anonymous callers).</summary>
public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public bool Enabled { get; set; } = true;

    [Range(1, 100000)]
    public int GlobalPermitPerMinute { get; set; } = 300;

    [Range(1, 1000)]
    public int AuthPermitPer15Minutes { get; set; } = 10;
}

/// <summary><c>Hosting</c> section.</summary>
public sealed class HostingOptions
{
    public const string SectionName = "Hosting";

    /// <summary>Serve the SPA from <c>wwwroot</c> (single deployment, no CORS).</summary>
    public bool ServeSpa { get; set; } = true;

    /// <summary>Expose <c>/openapi/v1.json</c> and <c>/scalar</c>. Defaults to Development only.</summary>
    public bool? EnableApiExplorer { get; set; }

    /// <summary>Redirect HTTP to HTTPS. Disable when TLS terminates at a reverse proxy.</summary>
    public bool UseHttpsRedirection { get; set; }

    /// <summary>Trusted proxies (IP addresses) for <c>X-Forwarded-*</c> headers.</summary>
    public string[] KnownProxies { get; set; } = [];

    /// <summary>Trusted proxy networks in CIDR notation, for example <c>10.0.0.0/8</c>.</summary>
    public string[] KnownNetworks { get; set; } = [];
}
