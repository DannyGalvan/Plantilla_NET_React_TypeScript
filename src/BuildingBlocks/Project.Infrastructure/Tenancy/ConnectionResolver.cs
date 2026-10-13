using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Project.Application.Tenancy;
using Project.Infrastructure.Persistence;
using Project.Infrastructure.Persistence.Providers;

namespace Project.Infrastructure.Tenancy;

/// <summary>
/// Resolves the engine and connection string of a module for the current tenant. Every DbContext and every
/// raw connection goes through here; nothing reads connection strings from configuration directly.
/// </summary>
public interface ITenantConnectionResolver
{
    (IDatabaseProvider Provider, string ConnectionString) Resolve(string module, DataScope scope);

    /// <summary>Loads (and caches) the secret of a dedicated tenant before its connection is used.</summary>
    ValueTask PrepareAsync(TenantInfo info, CancellationToken ct);
}

public sealed class TenantConnectionResolver(
    IOptions<DatabaseOptions> database,
    IOptions<TenancyOptions> tenancy,
    DatabaseProviderRegistry providers,
    TenantContext tenant,
    ISecretStore secrets,
    DedicatedConnectionCache cache) : ITenantConnectionResolver
{
    public (IDatabaseProvider Provider, string ConnectionString) Resolve(string module, DataScope scope)
    {
        if (scope == DataScope.Tenant && tenant.UsesDedicatedDatabase)
        {
            var info = tenant.Tenant!;
            if (!cache.TryGet(info.Id, out var dedicated))
            {
                throw new InvalidOperationException(
                    $"The connection of tenant '{info.Code}' was not prepared. Call PrepareAsync after resolving the tenant.");
            }

            return (providers.Get(info.DatabaseProvider ?? database.Value.Provider), dedicated);
        }

        var connections = database.Value.ConnectionStrings;
        var connection = (connections.GetValueOrDefault(module), scope) switch
        {
            ({ Length: > 0 } own, _) => own,
            (_, DataScope.Platform) when connections.GetValueOrDefault("Platform") is { Length: > 0 } platform => platform,
            _ => connections.GetValueOrDefault("Default"),
        };

        if (string.IsNullOrWhiteSpace(connection))
        {
            throw new InvalidOperationException(
                $"Missing connection string for module '{module}'. Set Database:ConnectionStrings:Default (user-secrets or environment).");
        }

        return (providers.Get(database.Value.Provider), connection);
    }

    public async ValueTask PrepareAsync(TenantInfo info, CancellationToken ct)
    {
        var dedicated = tenancy.Value.Mode is TenancyMode.DatabasePerTenant or TenancyMode.Hybrid
            && info.Isolation == TenantIsolation.Dedicated;

        if (!dedicated || cache.TryGet(info.Id, out _))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(info.ConnectionSecretRef))
        {
            throw new InvalidOperationException($"Tenant '{info.Code}' is dedicated but has no ConnectionSecretRef.");
        }

        var connection = await secrets.GetAsync(info.ConnectionSecretRef, ct)
            ?? throw new InvalidOperationException($"Secret '{info.ConnectionSecretRef}' of tenant '{info.Code}' was not found.");
        cache.Set(info.Id, connection);
    }
}

/// <summary>Process-wide cache of resolved dedicated connection strings.</summary>
public sealed class DedicatedConnectionCache
{
    private readonly ConcurrentDictionary<Guid, string> _connections = new();

    public bool TryGet(Guid tenantId, out string connection) => _connections.TryGetValue(tenantId, out connection!);

    public void Set(Guid tenantId, string connection) => _connections[tenantId] = connection;

    public void Remove(Guid tenantId) => _connections.TryRemove(tenantId, out _);
}

/// <summary>
/// Development and simple deployments: secrets come from configuration (<c>Secrets:{reference}</c>), which maps to
/// user-secrets or environment variables (<c>Secrets__tenant-acme-db</c>). Replace with Key Vault, AWS Secrets
/// Manager or Vault by registering another <see cref="ISecretStore"/>.
/// </summary>
public sealed class ConfigurationSecretStore(IConfiguration configuration) : ISecretStore
{
    public ValueTask<string?> GetAsync(string reference, CancellationToken ct) =>
        ValueTask.FromResult(configuration[$"Secrets:{reference}"]);
}
