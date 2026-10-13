namespace Project.Application.Settings;

/// <summary>
/// A settings section resolved in cascade: code defaults, then <c>appsettings</c> (<c>Settings:{Key}</c>),
/// then System, Tenant, organization units (root to active) and finally User. The most specific value wins.
/// </summary>
public interface ISettingsSection
{
    static abstract string SectionKey { get; }

    /// <summary>Whether a user may override the section for himself.</summary>
    static virtual bool AllowUserOverride => false;
}

public enum SettingScope
{
    System,
    Tenant,
    OrganizationUnit,
    User,
}

public interface ISettingsProvider
{
    ValueTask<T> GetAsync<T>(CancellationToken ct)
        where T : class, ISettingsSection, new();
}

/// <summary>Locale, time zone and currency used for formatting and documents.</summary>
public sealed class LocalizationSettings : ISettingsSection
{
    public static string SectionKey => "localization";

    public static bool AllowUserOverride => true;

    public string Locale { get; set; } = "es";

    public string TimeZone { get; set; } = "UTC";

    public string Currency { get; set; } = "USD";

    public string DateFormat { get; set; } = "dd/MM/yyyy";
}

/// <summary>Brand applied to the UI. Resolved by host so a single build serves many brands.</summary>
public sealed class BrandingSettings : ISettingsSection
{
    public static string SectionKey => "branding";

    public string DisplayName { get; set; } = "Plantilla";

    public string? LogoUrl { get; set; }

    public string? FaviconUrl { get; set; }

    public string ThemePreset { get; set; } = "default";

    /// <summary>Optional primary color (hex or OKLCH). Validated server side.</summary>
    public string? PrimaryColor { get; set; }

    public string Radius { get; set; } = "0.625rem";

    public bool AllowUserPrimaryOverride { get; set; } = true;
}

public sealed class TaxSettings : ISettingsSection
{
    public static string SectionKey => "tax";

    public string Country { get; set; } = "US";

    public string TaxIdLabel { get; set; } = "Tax ID";

    public decimal DefaultRate { get; set; }
}

public sealed class SecuritySettings : ISettingsSection
{
    public static string SectionKey => "security";

    public int PasswordMinLength { get; set; } = 10;

    public int PasswordHistoryLimit { get; set; } = 5;

    public int MaxFailedAccessAttempts { get; set; } = 5;

    public int LockoutMinutes { get; set; } = 15;

    public bool MfaRequired { get; set; }
}

/// <summary>Feature flags evaluated per tenant (and plan).</summary>
public interface IFeatureFlags
{
    ValueTask<bool> IsEnabledAsync(string feature, CancellationToken ct);

    ValueTask<IReadOnlyList<string>> GetEnabledAsync(CancellationToken ct);
}
