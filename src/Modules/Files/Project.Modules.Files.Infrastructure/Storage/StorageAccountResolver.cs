using System.Text.Json;
using Microsoft.Extensions.Options;
using Project.Application.Abstractions;
using Project.Application.Security;
using Project.Modules.Files.Application;
using Project.Modules.Files.Domain;

namespace Project.Modules.Files.Infrastructure.Storage;

/// <summary>The default connection of the tenant, or the platform storage from <c>Files:Default</c>.</summary>
public sealed class StorageAccountResolver(
    IRepository<StorageConnection, Guid> connections,
    ISecretProtector protector,
    IOptions<FilesOptions> options) : IStorageAccountResolver
{
    private const string CredentialsPurpose = "files.storage-credentials";

    public async Task<StorageAccount> ResolveDefaultAsync(CancellationToken ct)
    {
        var connection = await connections.FirstOrDefaultAsync(new Specification<StorageConnection>().Where(c => c.IsDefault), ct);
        return connection is null ? Platform() : FromConnection(connection);
    }

    public async Task<StorageAccount?> ResolveAsync(string provider, Guid? connectionId, CancellationToken ct)
    {
        if (connectionId is null)
        {
            // Platform storage: only readable while the platform still uses that provider.
            return string.Equals(options.Value.Default.Provider, provider, StringComparison.Ordinal) ? Platform() : null;
        }

        var connection = await connections.GetByIdAsync(connectionId.Value, ct);
        return connection is null ? null : FromConnection(connection);
    }

    private StorageAccount Platform()
    {
        var platform = options.Value.Default;
        return new StorageAccount(null, platform.Provider, platform.Settings, platform.Credentials);
    }

    private StorageAccount FromConnection(StorageConnection connection)
    {
        var settings = JsonSerializer.Deserialize<Dictionary<string, string>>(connection.Settings) ?? [];
        var json = protector.Unprotect(CredentialsPurpose, connection.ProtectedCredentials)
            ?? throw new InvalidOperationException($"The credentials of storage connection {connection.Id} cannot be decrypted.");
        var credentials = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? [];
        var id = connection.Id;

        return new StorageAccount(id, connection.Provider, settings, credentials)
        {
            OnCredentialsChanged = async (updated, ct) =>
            {
                var tracked = await connections.FirstOrDefaultAsync(new Specification<StorageConnection>().Where(c => c.Id == id).Tracked(), ct);
                if (tracked is not null)
                {
                    tracked.ReplaceCredentials(protector.Protect(CredentialsPurpose, JsonSerializer.Serialize(updated)));
                    await connections.UnitOfWork.SaveChangesAsync(ct);
                }
            },
        };
    }
}
