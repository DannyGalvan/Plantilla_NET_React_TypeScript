using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Project.Application.Abstractions;
using Project.Application.Configuration;
using Project.Application.Messaging;
using Project.Application.Security;
using Project.Modules.Configuration.Contracts;
using Project.Modules.Configuration.Domain;
using Project.SharedKernel;

namespace Project.Modules.Configuration.Application;

public enum FieldKind
{
    Text,
    Number,
    Boolean,
    List,
    Map,
}

/// <summary>One editable value of a section, discovered from the public properties of its options class.</summary>
public sealed record ConfigurationField(
    string Key,
    string Section,
    FieldKind Kind,
    bool Secret,
    bool RequiresRestart,
    string? Help,
    object? Default,
    PropertyInfo? Property);

/// <summary>
/// The fields of every section declared with <c>AddConfigurableSection</c>, flattened to configuration keys
/// (<c>Files:OAuth:Google:ClientId</c>). Nested classes become prefixes; string arrays are lists; dictionaries are maps.
/// </summary>
public sealed class ConfigurationCatalog(IEnumerable<ConfigurableSection> sections)
{
    public IReadOnlyList<ConfigurableSection> Sections { get; } = sections.OrderBy(s => s.Section, StringComparer.Ordinal).ToList();

    public IReadOnlyList<ConfigurationField> Fields(ConfigurableSection section)
    {
        var fields = new List<ConfigurationField>();
        var defaults = Activator.CreateInstance(section.OptionsType);
        var secret = section.OptionsType.GetCustomAttribute<SecretAttribute>() is not null;
        var restart = section.OptionsType.GetCustomAttribute<RequiresRestartAttribute>() is not null;

        if (MapValueType(section.OptionsType) is not null)
        {
            fields.Add(new ConfigurationField(section.Section, section.Section, FieldKind.Map, secret, restart,
                section.OptionsType.GetCustomAttribute<ConfigurationHelpAttribute>()?.Text, secret ? null : defaults, null));
            return fields;
        }

        Collect(section.Section, section.Section, section.OptionsType, defaults, secret, restart, fields, depth: 0);
        return fields;
    }

    public ConfigurableSection? Find(string section) =>
        Sections.FirstOrDefault(s => string.Equals(s.Section, section, StringComparison.OrdinalIgnoreCase));

    private static void Collect(string section, string prefix, Type type, object? defaults, bool secret, bool restart, List<ConfigurationField> fields, int depth)
    {
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead && p.GetIndexParameters().Length == 0))
        {
            var key = $"{prefix}:{property.Name}";
            var isSecret = secret || property.GetCustomAttribute<SecretAttribute>() is not null;
            var isRestart = restart || property.GetCustomAttribute<RequiresRestartAttribute>() is not null;
            var help = property.GetCustomAttribute<ConfigurationHelpAttribute>()?.Text;
            var value = defaults is null ? null : property.GetValue(defaults);
            var shown = isSecret ? null : value;
            var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

            if (KindOf(propertyType) is { } kind)
            {
                if (property.CanWrite)
                {
                    fields.Add(new ConfigurationField(key, section, kind, isSecret, isRestart, help, shown, property));
                }
            }
            else if (propertyType == typeof(string[]) || propertyType == typeof(List<string>))
            {
                fields.Add(new ConfigurationField(key, section, FieldKind.List, isSecret, isRestart, help, shown, property));
            }
            else if (MapValueType(propertyType) is not null)
            {
                fields.Add(new ConfigurationField(key, section, FieldKind.Map, isSecret, isRestart, help, shown, property));
            }
            else if (propertyType.IsClass && depth < 4)
            {
                Collect(section, key, propertyType, value, isSecret, isRestart, fields, depth + 1);
            }
        }
    }

    private static FieldKind? KindOf(Type type) =>
        type == typeof(string) || type.IsEnum ? FieldKind.Text
        : type == typeof(bool) ? FieldKind.Boolean
        : type == typeof(int) || type == typeof(long) || type == typeof(decimal) || type == typeof(double) ? FieldKind.Number
        : null;

    private static Type? MapValueType(Type type) =>
        type.GetInterfaces().Append(type)
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>) && i.GetGenericArguments()[0] == typeof(string))
            ?.GetGenericArguments()[1];
}

public sealed record ConfigurationFieldDto(
    string Key,
    string Kind,
    bool Secret,
    bool RequiresRestart,
    string? Help,
    object? Default,
    object? Value,
    bool HasValue,
    string? Source);

public sealed record ConfigurationSectionDto(string Section, string Module, string? Description, IReadOnlyList<ConfigurationFieldDto> Fields);

public sealed record ConfigurationOverviewDto(bool SecretsAvailable, IReadOnlyList<ConfigurationSectionDto> Sections);

public sealed record GetConfigurationQuery : IQuery<ConfigurationOverviewDto>, IRequirePermission
{
    public string Permission => ConfigurationPermissions.Platform.Manage;
}

/// <summary>Every section with its effective values; secrets only report whether they are set.</summary>
public sealed class GetConfigurationHandler(ConfigurationCatalog catalog, IConfigurationInspector configuration, ISecretCipher cipher)
    : IQueryHandler<GetConfigurationQuery, ConfigurationOverviewDto>
{
    public ValueTask<Result<ConfigurationOverviewDto>> Handle(GetConfigurationQuery query, CancellationToken cancellationToken)
    {
        IReadOnlyList<ConfigurationSectionDto> sections = catalog.Sections
            .Select(section => new ConfigurationSectionDto(
                section.Section,
                section.Module,
                section.Description,
                catalog.Fields(section).Select(field => Describe(field, configuration)).ToList()))
            .ToList();
        return ValueTask.FromResult(Result<ConfigurationOverviewDto>.Success(new ConfigurationOverviewDto(cipher.IsAvailable, sections)));
    }

    private static ConfigurationFieldDto Describe(ConfigurationField field, IConfigurationInspector configuration)
    {
        object? value;
        bool hasValue;
        switch (field.Kind)
        {
            case FieldKind.List:
                var items = configuration.Children(field.Key)
                    .OrderBy(k => int.TryParse(k, out var i) ? i : int.MaxValue)
                    .Select(k => configuration.Get($"{field.Key}:{k}"))
                    .ToList();
                hasValue = items.Count > 0;
                value = field.Secret ? null : items;
                break;
            case FieldKind.Map:
                var names = configuration.Children(field.Key);
                hasValue = names.Count > 0;

                // Secret maps show the names (for example which tenant connections exist), never the values.
                value = names.ToDictionary(n => n, n => field.Secret ? null : configuration.Get($"{field.Key}:{n}"));
                break;
            default:
                var text = configuration.Get(field.Key);
                hasValue = !string.IsNullOrEmpty(text);
                value = field.Secret ? null : text;
                break;
        }

        return new ConfigurationFieldDto(
            field.Key,
            field.Kind.ToString(),
            field.Secret,
            field.RequiresRestart,
            field.Help,
            field.Default,
            value,
            hasValue,
            configuration.SourceOf(field.Key));
    }
}

/// <summary>
/// Saves values of one section. Per field: <c>null</c> removes the value (the default applies again); a list replaces
/// every item; a map changes only the names it contains (a <c>null</c> value removes that name).
/// </summary>
public sealed record UpdateConfigurationCommand(string Section, Dictionary<string, JsonElement> Values) : ICommand, IRequirePermission
{
    public string Permission => ConfigurationPermissions.Platform.Manage;
}

public sealed class UpdateConfigurationHandler(
    ConfigurationCatalog catalog,
    IRepository<ConfigurationEntry, Guid> entries,
    ISecretCipher cipher,
    IConfigurationInspector configuration) : ICommandHandler<UpdateConfigurationCommand>
{
    public async ValueTask<Result> Handle(UpdateConfigurationCommand command, CancellationToken cancellationToken)
    {
        var section = catalog.Find(command.Section);
        if (section is null)
        {
            return Error.NotFound("configuration.section_not_found");
        }

        var fields = catalog.Fields(section).ToDictionary(f => f.Key, StringComparer.OrdinalIgnoreCase);
        var writes = new Dictionary<string, (string? Value, bool Secret)>(StringComparer.OrdinalIgnoreCase);
        var removals = new List<string>();
        var errors = new Dictionary<string, string[]>();

        foreach (var (key, value) in command.Values)
        {
            if (!fields.TryGetValue(key, out var field))
            {
                errors[key] = ["This key is not part of the section."];
                continue;
            }

            if (Plan(field, value, writes, removals) is { } problem)
            {
                errors[field.Key] = [problem];
            }
        }

        if (writes.Any(w => w.Value.Secret && w.Value.Value is not null) && !cipher.IsAvailable)
        {
            return Error.Validation("configuration.no_master_key",
                "Secrets cannot be stored: configure Configuration:MasterKey (environment variable Configuration__MasterKey).");
        }

        if (errors.Count > 0)
        {
            return Error.Validation("configuration.invalid", "Some values are not valid.", errors);
        }

        var prefixes = removals.Select(r => r + ":").ToList();
        var affected = writes.Keys.Concat(removals).ToList();
        var existing = (await entries.ListAsync(new Specification<ConfigurationEntry>().Tracked(), cancellationToken))
            .Where(e => affected.Contains(e.Key, StringComparer.OrdinalIgnoreCase) || prefixes.Any(p => e.Key.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        foreach (var entry in existing.Where(e => !writes.ContainsKey(e.Key)))
        {
            entries.Remove(entry);
        }

        foreach (var (key, (value, secret)) in writes)
        {
            var stored = secret && value is not null ? cipher.Encrypt(value) : value;
            if (existing.FirstOrDefault(e => string.Equals(e.Key, key, StringComparison.OrdinalIgnoreCase)) is { } entry)
            {
                entry.Change(stored, secret);
            }
            else
            {
                entries.Add(ConfigurationEntry.Create(key, stored, secret));
            }
        }

        await entries.UnitOfWork.SaveChangesAsync(cancellationToken);
        configuration.Reload();
        return Result.Success();
    }

    /// <summary>Turns one field of the request into rows to write and prefixes to remove. Null when valid.</summary>
    private static string? Plan(ConfigurationField field, JsonElement value, Dictionary<string, (string?, bool)> writes, List<string> removals)
    {
        if (value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            removals.Add(field.Key);
            if (field.Kind is not (FieldKind.List or FieldKind.Map))
            {
                writes.Remove(field.Key);
            }

            return null;
        }

        switch (field.Kind)
        {
            case FieldKind.List:
                if (value.ValueKind != JsonValueKind.Array || value.EnumerateArray().Any(i => i.ValueKind != JsonValueKind.String))
                {
                    return "Expected a list of texts.";
                }

                removals.Add(field.Key);
                var index = 0;
                foreach (var item in value.EnumerateArray().Select(i => i.GetString()!.Trim()).Where(i => i.Length > 0))
                {
                    writes[$"{field.Key}:{index++}"] = (item, field.Secret);
                }

                return null;

            case FieldKind.Map:
                if (value.ValueKind != JsonValueKind.Object)
                {
                    return "Expected an object of names and values.";
                }

                foreach (var pair in value.EnumerateObject())
                {
                    var name = pair.Name.Trim();
                    if (name.Length == 0 || name.Contains(':', StringComparison.Ordinal))
                    {
                        return $"'{pair.Name}' is not a valid name.";
                    }

                    if (pair.Value.ValueKind == JsonValueKind.Null)
                    {
                        removals.Add($"{field.Key}:{name}");
                        continue;
                    }

                    writes[$"{field.Key}:{name}"] = (Scalar(pair.Value), field.Secret);
                }

                return null;

            default:
                var text = Scalar(value);
                if (Convert(field, text, out var converted) is { } problem)
                {
                    return problem;
                }

                // The options class validates the value with its own attributes (Range, EmailAddress...).
                foreach (var attribute in field.Property?.GetCustomAttributes<ValidationAttribute>() ?? [])
                {
                    if (!attribute.IsValid(converted))
                    {
                        return attribute.FormatErrorMessage(field.Property!.Name);
                    }
                }

                writes[field.Key] = (text, field.Secret);
                return null;
        }
    }

    private static string? Convert(ConfigurationField field, string text, out object? converted)
    {
        converted = text;
        var type = field.Property is null ? typeof(string) : Nullable.GetUnderlyingType(field.Property.PropertyType) ?? field.Property.PropertyType;
        switch (field.Kind)
        {
            case FieldKind.Boolean:
                if (!bool.TryParse(text, out var flag))
                {
                    return "Expected true or false.";
                }

                converted = flag;
                return null;
            case FieldKind.Number:
                if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
                {
                    return "Expected a number.";
                }

                if ((type == typeof(int) || type == typeof(long)) && number != decimal.Truncate(number))
                {
                    return "Expected a whole number.";
                }

                converted = type == typeof(int) ? (object)(int)number : type == typeof(long) ? (long)number : number;
                return null;
            default:
                if (type.IsEnum && !Enum.TryParse(type, text, ignoreCase: true, out _))
                {
                    return $"Expected one of: {string.Join(", ", Enum.GetNames(type))}.";
                }

                return null;
        }
    }

    private static string Scalar(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString()!.Trim(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        _ => value.GetRawText(),
    };
}

/// <summary>Permissions and navigation of the Configuration module.</summary>
public sealed class ConfigurationPermissionProvider : IPermissionProvider
{
    public IEnumerable<PermissionDefinition> GetPermissions() =>
    [
        new(ConfigurationPermissions.Platform.Manage, ConfigurationPermissions.Module, "permissions.configuration.platform.manage", "platform", PlatformOnly: true),
    ];

    public IEnumerable<NavigationDefinition> GetNavigation() =>
    [
        new("configuration.platform", ConfigurationPermissions.Module, "nav.platformConfiguration", "/configuration", "sliders-horizontal", ConfigurationPermissions.Platform.Manage, 920),
    ];
}
