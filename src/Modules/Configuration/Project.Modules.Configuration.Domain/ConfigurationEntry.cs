using Project.SharedKernel.Domain;

namespace Project.Modules.Configuration.Domain;

/// <summary>
/// One configuration value of the platform (<c>Email:Host</c>, <c>Files:OAuth:Google:ClientSecret</c>...). Secrets are
/// stored encrypted with the master key; <see cref="DisplayValue"/> keeps what the change history may show.
/// </summary>
public sealed class ConfigurationEntry : AggregateRoot<Guid>, IAuditable, IHasConcurrencyStamp
{
    private ConfigurationEntry()
    {
    }

    public string Key { get; private set; } = string.Empty;

    /// <summary>Plain value, or the encrypted value of a secret. Read without EF by the configuration provider.</summary>
    [AuditIgnore]
    public string? Value { get; private set; }

    public bool IsSecret { get; private set; }

    /// <summary>The value for the change history: masked for secrets.</summary>
    public string? DisplayValue { get; private set; }

    public DateTimeOffset CreatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public string? UpdatedBy { get; set; }

    public Guid ConcurrencyStamp { get; set; }

    public static ConfigurationEntry Create(string key, string? value, bool isSecret) =>
        new ConfigurationEntry { Id = NewId.Next(), Key = key }.With(value, isSecret);

    public void Change(string? value, bool isSecret) => With(value, isSecret);

    private ConfigurationEntry With(string? value, bool isSecret)
    {
        Value = value;
        IsSecret = isSecret;
        DisplayValue = isSecret ? "********" : value is { Length: > 500 } ? value[..500] : value;
        return this;
    }
}
