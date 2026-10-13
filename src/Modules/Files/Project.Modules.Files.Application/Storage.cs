using Project.Application.Settings;

namespace Project.Modules.Files.Application;

/// <summary>
/// Storage account used for one operation: the default platform storage or a connection of the tenant, with its
/// settings and decrypted credentials. Never logged, never returned by the API.
/// </summary>
public sealed class StorageAccount(
    Guid? connectionId,
    string provider,
    IReadOnlyDictionary<string, string> settings,
    IReadOnlyDictionary<string, string> credentials)
{
    public Guid? ConnectionId { get; } = connectionId;

    public string Provider { get; } = provider;

    public IReadOnlyDictionary<string, string> Settings { get; } = settings;

    public IReadOnlyDictionary<string, string> Credentials { get; private set; } = credentials;

    /// <summary>Persists rotated credentials (Microsoft rotates refresh tokens on every use).</summary>
    public Func<IReadOnlyDictionary<string, string>, CancellationToken, Task>? OnCredentialsChanged { get; init; }

    public string? Setting(string key) => Settings.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;

    public string? Credential(string key) => Credentials.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;

    public async Task UpdateCredentialsAsync(IReadOnlyDictionary<string, string> credentials, CancellationToken ct)
    {
        Credentials = credentials;
        if (OnCredentialsChanged is not null)
        {
            await OnCredentialsChanged(credentials, ct);
        }
    }
}

/// <summary>
/// A storage provider (strategy). Adding one (Dropbox, Box, SFTP...) is a new class registered in DI; nothing else
/// changes.
/// </summary>
public interface IFileStorage
{
    /// <summary>One of <c>StorageProviders</c>.</summary>
    string Provider { get; }

    /// <summary>Whether tenants may connect their own account of this provider (local disk is platform only).</summary>
    bool AllowsTenantConnections { get; }

    /// <summary>Checks the settings and credentials of a new connection. Null when valid.</summary>
    string? Validate(IReadOnlyDictionary<string, string> settings, IReadOnlyDictionary<string, string> credentials);

    /// <summary>Stores the content and returns its location (object key or provider item id).</summary>
    Task<string> SaveAsync(StorageAccount account, StoredObject file, Stream content, CancellationToken ct);

    Task<Stream> OpenReadAsync(StorageAccount account, string location, CancellationToken ct);

    Task DeleteAsync(StorageAccount account, string location, CancellationToken ct);
}

/// <summary>What a provider needs to name the stored object.</summary>
public sealed record StoredObject(Guid Id, Guid TenantId, string FileName, string ContentType, long Length, DateTimeOffset CreatedAt)
{
    /// <summary>Key for object stores: tenant prefix keeps tenants apart inside a shared bucket.</summary>
    public string Key => $"{TenantId:N}/{CreatedAt:yyyy}/{CreatedAt:MM}/{Id:N}{Path.GetExtension(FileName).ToLowerInvariant()}";
}

/// <summary>Providers connected with an OAuth consent screen (Google Drive, OneDrive).</summary>
public interface IOAuthStorageProvider
{
    string Provider { get; }

    /// <summary>False when the platform has no OAuth client for this provider (the UI hides the button).</summary>
    bool IsConfigured { get; }

    string BuildAuthorizationUrl(string state, string redirectUri);

    Task<OAuthGrant> ExchangeCodeAsync(string code, string redirectUri, CancellationToken ct);
}

public sealed record OAuthGrant(IReadOnlyDictionary<string, string> Credentials, string? Account);

/// <summary>Resolves the storage account of the current tenant.</summary>
public interface IStorageAccountResolver
{
    /// <summary>The default connection of the tenant, or the platform storage (<c>Files:Default</c>).</summary>
    Task<StorageAccount> ResolveDefaultAsync(CancellationToken ct);

    /// <summary>The account a stored file was written to. Null when the connection no longer exists.</summary>
    Task<StorageAccount?> ResolveAsync(string provider, Guid? connectionId, CancellationToken ct);
}

/// <summary>Upload limits, editable per tenant and organization unit.</summary>
public sealed class FileSettings : ISettingsSection
{
    public static string SectionKey => "files";

    public int MaxFileSizeMb { get; set; } = 25;

    /// <summary>Deleted files stay restorable this many days; then the content is removed from the storage. 0 keeps them.</summary>
    public int PurgeDeletedAfterDays { get; set; } = 30;

    public string[] AllowedExtensions { get; set; } =
        [".pdf", ".png", ".jpg", ".jpeg", ".gif", ".webp", ".txt", ".csv", ".xlsx", ".xls", ".docx", ".doc", ".pptx", ".zip", ".xml", ".json"];
}
