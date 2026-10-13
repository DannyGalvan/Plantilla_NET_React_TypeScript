using Project.Application.Configuration;

namespace Project.Modules.Files.Infrastructure.Storage;

/// <summary>Platform configuration of the Files module (section <c>Files</c>).</summary>
public sealed class FilesOptions
{
    public const string SectionName = "Files";

    /// <summary>Storage used by tenants without a connection of their own.</summary>
    public DefaultStorageOptions Default { get; set; } = new();

    /// <summary>
    /// Allows tenant connections to custom endpoints (MinIO, Azurite). Off by default: a tenant could otherwise point
    /// the server to internal addresses (SSRF).
    /// </summary>
    public bool AllowCustomEndpoints { get; set; }

    /// <summary>Largest request accepted by the upload endpoint, in MB (the per-tenant limit is the <c>files</c> setting).</summary>
    [RequiresRestart]
    public int MaxRequestSizeMb { get; set; } = 100;

    public OAuthOptions OAuth { get; set; } = new();
}

public sealed class DefaultStorageOptions
{
    public string Provider { get; set; } = "local";

    /// <summary>Non-secret settings, for example <c>rootPath</c>, <c>bucket</c> or <c>container</c>.</summary>
    public Dictionary<string, string> Settings { get; set; } = [];

    /// <summary>Keys of the platform account (encrypted in the database).</summary>
    [Secret]
    public Dictionary<string, string> Credentials { get; set; } = [];
}

/// <summary>OAuth clients registered by the platform owner in Google Cloud and Microsoft Entra.</summary>
public sealed class OAuthOptions
{
    /// <summary>
    /// Callback registered in both consoles, for example <c>https://app.example.com/api/v1/files/oauth/callback</c>.
    /// When empty it is derived from the request (fine for a single host).
    /// </summary>
    public string? RedirectUri { get; set; }

    public OAuthClientOptions Google { get; set; } = new();

    public OAuthClientOptions Microsoft { get; set; } = new();
}

public sealed class OAuthClientOptions
{
    public string? ClientId { get; set; }

    [Secret]
    public string? ClientSecret { get; set; }

    /// <summary>Microsoft only: <c>common</c> (personal and work accounts), <c>organizations</c> or a tenant id.</summary>
    public string Tenant { get; set; } = "common";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);
}
