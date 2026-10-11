namespace Project.Modules.Tenancy.Application;

/// <summary>
/// Tenant catalog. Reads go through HybridCache with the \`tenant:{id}\` tag;
/// writes invalidate the tag via \`TenantChanged\`.
/// </summary>
public interface ITenantCatalog
{
    ValueTask<TenantInfo?> FindByCodeAsync(string code, CancellationToken ct);
    ValueTask<TenantInfo?> FindByHostAsync(string host, CancellationToken ct);
    IAsyncEnumerable<TenantInfo> ListActiveAsync(CancellationToken ct);
    Task InvalidateAsync(Guid tenantId, CancellationToken ct);
}

public sealed record TenantInfo(
    Guid Id,
    string Code,
    string Name,
    string Status,
    string Isolation,
    string? DatabaseProvider,
    string? ConnectionSecretRef,
    string? Plan,
    IReadOnlySet<string> Features);

public interface ITenantManager
{
    Task<TenantInfo> CreateAsync(CreateTenantCommand command, CancellationToken ct);
    Task ProvisionAsync(Guid tenantId, CancellationToken ct);
    Task SuspendAsync(Guid tenantId, CancellationToken ct);
    Task AddDomainAsync(Guid tenantId, string host, bool isPrimary, CancellationToken ct);
    Task SetFeatureAsync(Guid tenantId, string feature, bool enabled, CancellationToken ct);
}

public sealed record CreateTenantCommand(
    string Code,
    string Name,
    string Isolation,
    string? DatabaseProvider,
    string? ConnectionSecretRef,
    string? Plan);

public interface ITenantSettingStore
{
    ValueTask<string?> GetAsync(string scope, Guid? tenantId, Guid? orgUnitId, Guid? userId, string sectionKey, CancellationToken ct);
    Task SetAsync(string scope, Guid? tenantId, Guid? orgUnitId, Guid? userId, string sectionKey, string json, CancellationToken ct);
}

/// <summary>
/// Cascading settings resolver. System → Tenant → OrganizationUnit (root →
/// active) → User. Each layer is JSON-merged on top of the previous one;
/// the most specific wins on each property.
/// </summary>
public interface ISettingsProvider
{
    ValueTask<T> GetAsync<T>(CancellationToken ct) where T : class, ISettingsSection, new();
}

public interface ISettingsSection
{
    static abstract string SectionKey { get; }
}

public sealed class LocalizationSettings : ISettingsSection
{
    public string Locale { get; init; } = "en";
    public string TimeZone { get; init; } = "UTC";
    public string Currency { get; init; } = "USD";
    public string DateFormat { get; init; } = "yyyy-MM-dd";
    public static string SectionKey => "localization";
}

public sealed class BrandingSettings : ISettingsSection
{
    public string DisplayName { get; init; } = "Plantilla";
    public string? LogoUrl { get; init; }
    public string? FaviconUrl { get; init; }
    public string ThemePreset { get; init; } = "default";
    public string? PrimaryColor { get; init; }
    public string? Radius { get; init; }
    public static string SectionKey => "branding";
}

public sealed class TaxSettings : ISettingsSection
{
    public string Country { get; init; } = "US";
    public string TaxIdLabel { get; init; } = "Tax ID";
    public decimal DefaultRate { get; init; }
    public static string SectionKey => "tax";
}

public sealed class SecuritySettings : ISettingsSection
{
    public int PasswordMinLength { get; init; } = 8;
    public bool MfaRequired { get; init; }
    public int SessionTimeoutMinutes { get; init; } = 60;
    public static string SectionKey => "security";
}

public sealed class FeatureSettings : ISettingsSection
{
    public bool AdvancedPricing { get; init; }
    public bool MultiCurrency { get; init; }
    public bool AuditTrail { get; init; } = true;
    public static string SectionKey => "features";
}

public interface IFeatureFlags
{
    ValueTask<bool> IsEnabledAsync(string feature, CancellationToken ct);
}