using System.ComponentModel.DataAnnotations;
using Project.Application.Tenancy;

namespace Project.Infrastructure.Tenancy;

/// <summary><c>Tenancy</c> configuration section.</summary>
public sealed class TenancyOptions
{
    public const string SectionName = "Tenancy";

    public TenancyMode Mode { get; set; } = TenancyMode.None;

    /// <summary>
    /// Ordered resolution strategies: <c>Host</c> (custom domain), <c>Subdomain</c>, <c>Claim</c> (<c>tid</c>),
    /// <c>Header</c> (<c>X-Tenant</c>, internal services only).
    /// </summary>
    public List<string> Resolvers { get; set; } = ["Host", "Subdomain", "Claim"];

    /// <summary>Root domain for subdomain resolution: <c>acme.app.local</c> resolves tenant <c>acme</c>.</summary>
    public string? RootDomain { get; set; }

    [Required]
    public string HeaderName { get; set; } = "X-Tenant";

    /// <summary>Maximum pool size added to dedicated tenant connection strings.</summary>
    [Range(1, 1000)]
    public int MaxDedicatedPoolSize { get; set; } = 32;
}
