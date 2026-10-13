using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Project.Application.Configuration;

namespace Project.Web.Security;

/// <summary>
/// <c>Jwt</c> section, editable at runtime in the configuration page. <see cref="SigningKey"/> is generated on the
/// first start and stored encrypted in the platform database; changing it signs every user out.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; set; } = "project-api";

    [Required]
    public string Audience { get; set; } = "project-client";

    [Secret]
    [ConfigurationHelp("At least 32 characters. Generated automatically; changing it signs every user out.")]
    public string SigningKey { get; set; } = string.Empty;

    [Range(1, 1440)]
    public int AccessTokenMinutes { get; set; } = 15;

    [Range(1, 365)]
    public int RefreshTokenDays { get; set; } = 7;

    public SymmetricSecurityKey GetSecurityKey() =>
        SigningKey.Length >= 32
            ? new(Encoding.UTF8.GetBytes(SigningKey))
            : throw new InvalidOperationException(
                "Jwt:SigningKey is not configured. Set Configuration:MasterKey so it is generated and stored in the database, or provide Jwt__SigningKey.");
}

/// <summary><c>Cors</c> section. Only needed when the SPA is served from another origin.</summary>
public sealed class CorsOptions
{
    public const string SectionName = "Cors";

    [ConfigurationHelp("Origins allowed to call the API from a browser, for example https://app.example.com.")]
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

/// <summary><c>Hosting</c> section. Read at startup: it stays in appsettings or environment variables.</summary>
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
