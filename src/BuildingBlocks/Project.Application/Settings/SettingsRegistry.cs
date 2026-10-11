using Microsoft.Extensions.DependencyInjection;

namespace Project.Application.Settings;

/// <summary>Describes a settings section so it can be listed, edited and validated generically.</summary>
public sealed record SettingsSectionDescriptor(string Key, Type Type, bool AllowUserOverride)
{
    public object CreateDefault() => Activator.CreateInstance(Type)!;
}

public static class SettingsRegistrationExtensions
{
    /// <summary>
    /// Declares a settings section. Modules call this for their own sections; the settings editor and the
    /// cascading provider discover them without central changes.
    /// </summary>
    public static IServiceCollection AddSettingsSection<T>(this IServiceCollection services)
        where T : class, ISettingsSection, new()
    {
        var exists = services.Any(d => d.ServiceType == typeof(SettingsSectionDescriptor)
            && d.ImplementationInstance is SettingsSectionDescriptor existing
            && existing.Key == T.SectionKey);
        if (!exists)
        {
            services.AddSingleton(new SettingsSectionDescriptor(T.SectionKey, typeof(T), T.AllowUserOverride));
        }

        return services;
    }

    /// <summary>Registers the sections every application has (localization, branding, tax, security).</summary>
    public static IServiceCollection AddCoreSettingsSections(this IServiceCollection services) =>
        services
            .AddSettingsSection<LocalizationSettings>()
            .AddSettingsSection<BrandingSettings>()
            .AddSettingsSection<TaxSettings>()
            .AddSettingsSection<SecuritySettings>();
}
