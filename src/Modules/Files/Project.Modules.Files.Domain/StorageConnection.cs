using Project.SharedKernel;
using Project.SharedKernel.Domain;

namespace Project.Modules.Files.Domain;

/// <summary>
/// Storage account a tenant connected (Google Drive, OneDrive, S3, Azure Blob). Credentials are encrypted at rest
/// and never returned by the API; <see cref="Settings"/> only holds non-secret values (bucket, folder, region).
/// </summary>
public sealed class StorageConnection : AuditableAggregateRoot, IMultiTenant
{
    public Guid TenantId { get; set; }

    public string Name { get; private set; } = string.Empty;

    public string Provider { get; private set; } = string.Empty;

    /// <summary>Non-secret settings as JSON (<c>{"bucket":"files","region":"us-east-1"}</c>).</summary>
    public string Settings { get; private set; } = "{}";

    /// <summary>Credentials as encrypted JSON.</summary>
    public string ProtectedCredentials { get; private set; } = string.Empty;

    /// <summary>Account shown to administrators (email of the Drive owner, bucket name).</summary>
    public string? Account { get; private set; }

    /// <summary>New files of the tenant go to the default connection.</summary>
    public bool IsDefault { get; private set; }

    public static Result<StorageConnection> Create(string name, string provider, string settings, string protectedCredentials, string? account)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(provider))
        {
            return Error.Validation("name", "The connection needs a name and a provider.");
        }

        return new StorageConnection
        {
            Name = name.Trim(),
            Provider = provider,
            Settings = settings,
            ProtectedCredentials = protectedCredentials,
            Account = account,
        };
    }

    public void Rename(string name) => Name = name.Trim();

    public void MakeDefault(bool isDefault) => IsDefault = isDefault;

    /// <summary>Providers that rotate refresh tokens (Microsoft) store the new ones.</summary>
    public void ReplaceCredentials(string protectedCredentials) => ProtectedCredentials = protectedCredentials;
}
