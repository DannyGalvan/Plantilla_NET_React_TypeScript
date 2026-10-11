using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Project.Application.Security;
using Project.Application.Settings;
using Project.Modules.Tenancy.Application.Settings;
using Project.Modules.Tenancy.Contracts;
using Project.SharedKernel;

namespace Project.Modules.Tenancy.Application;

/// <summary>Permissions and navigation declared by the Tenancy module.</summary>
public sealed class TenancyPermissionProvider : IPermissionProvider
{
    public IEnumerable<PermissionDefinition> GetPermissions() =>
    [
        new(TenancyPermissions.Tenants.Read, TenancyPermissions.Module, "permissions.tenancy.tenants.read", "tenants"),
        new(TenancyPermissions.Tenants.Manage, TenancyPermissions.Module, "permissions.tenancy.tenants.manage", "tenants"),
        new(TenancyPermissions.Settings.Read, TenancyPermissions.Module, "permissions.tenancy.settings.read", "settings"),
        new(TenancyPermissions.Settings.Manage, TenancyPermissions.Module, "permissions.tenancy.settings.manage", "settings"),
    ];

    public IEnumerable<NavigationDefinition> GetNavigation() =>
    [
        new("tenancy.tenants", TenancyPermissions.Module, "nav.tenants", "/tenancy/tenants", "building-2", TenancyPermissions.Tenants.Read, 900, TenancyPermissions.Features.PlatformAdmin),
        new("tenancy.settings", TenancyPermissions.Module, "nav.settings", "/tenancy/settings", "settings", TenancyPermissions.Settings.Read, 910),
    ];
}

/// <summary>Type-checks section values and validates brand colors (hex or OKLCH) on the server.</summary>
public sealed partial class DefaultSettingsValidator : ISettingsValidator
{
    public Error? Validate(SettingsSectionDescriptor section, JsonObject values)
    {
        try
        {
            values.Deserialize(section.Type, SettingsJson.Options);
        }
        catch (JsonException)
        {
            return Error.Validation("values", $"The values are not valid for section '{section.Key}'.");
        }

        if (section.Type == typeof(BrandingSettings)
            && values.TryGetPropertyValue(nameof(BrandingSettings.PrimaryColor).ToCamel(), out var color)
            && color?.GetValue<string>() is { Length: > 0 } value
            && !ColorPattern().IsMatch(value))
        {
            return Error.Validation("primaryColor", "Use a hex color (#1d4ed8) or OKLCH (oklch(0.55 0.2 260)).");
        }

        return null;
    }

    [GeneratedRegex(@"^(#[0-9a-fA-F]{6}|oklch\(\s*[0-9.]+%?\s+[0-9.]+\s+[0-9.]+\s*\))$")]
    private static partial Regex ColorPattern();
}

internal static class StringExtensions
{
    public static string ToCamel(this string value) => char.ToLowerInvariant(value[0]) + value[1..];
}
