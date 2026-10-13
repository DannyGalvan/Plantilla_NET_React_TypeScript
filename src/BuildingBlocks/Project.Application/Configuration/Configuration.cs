using Microsoft.Extensions.DependencyInjection;

namespace Project.Application.Configuration;

/// <summary>The value is a secret: encrypted in the database and never returned by the API.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class)]
public sealed class SecretAttribute : Attribute;

/// <summary>The value is read when the application starts: a change applies after a restart (not a redeploy).</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class)]
public sealed class RequiresRestartAttribute : Attribute;

/// <summary>Help text shown next to a field in the configuration page.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class)]
public sealed class ConfigurationHelpAttribute(string text) : Attribute
{
    public string Text { get; } = text;
}

/// <summary>
/// A section of the platform configuration that administrators edit at runtime (stored in the platform database,
/// applied without redeploying). <see cref="OptionsType"/> is the options class bound to <see cref="Section"/>.
/// </summary>
public sealed record ConfigurableSection(string Section, Type OptionsType, string Module, string? Description);

public static class ConfigurableSectionExtensions
{
    /// <summary>
    /// Publishes an options class in the configuration page: <c>services.AddConfigurableSection&lt;EmailOptions&gt;("Email", "platform")</c>.
    /// Mark secrets with <see cref="SecretAttribute"/> and values read only at startup with <see cref="RequiresRestartAttribute"/>.
    /// Consumers read the current value with <c>IOptionsMonitor</c> (singletons) or <c>IOptionsSnapshot</c> (scoped).
    /// </summary>
    public static IServiceCollection AddConfigurableSection<TOptions>(this IServiceCollection services, string section, string module, string? description = null)
        where TOptions : class
    {
        var exists = services.Any(d => d.ServiceType == typeof(ConfigurableSection)
            && d.ImplementationInstance is ConfigurableSection existing
            && string.Equals(existing.Section, section, StringComparison.OrdinalIgnoreCase));
        if (!exists)
        {
            services.AddSingleton(new ConfigurableSection(section, typeof(TOptions), module, description));
        }

        return services;
    }
}

/// <summary>Encrypts secret configuration values with the master key (<c>Configuration:MasterKey</c>).</summary>
public interface ISecretCipher
{
    /// <summary>False when no master key is configured: secrets cannot be stored in the database.</summary>
    bool IsAvailable { get; }

    string Encrypt(string plaintext);
}

/// <summary>Configuration as the application sees it, with the origin of each value.</summary>
public interface IConfigurationInspector
{
    /// <summary><c>database</c>, <c>environment</c>, <c>file</c>, <c>other</c> or null when the default applies.</summary>
    string? SourceOf(string key);

    string? Get(string key);

    IReadOnlyList<string> Children(string key);

    /// <summary>Re-reads the database now on this instance (the others poll every few seconds).</summary>
    void Reload();
}
