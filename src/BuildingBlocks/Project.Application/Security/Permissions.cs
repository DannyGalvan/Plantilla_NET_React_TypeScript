namespace Project.Application.Security;

/// <summary>
/// Permission declared by a module. Keys follow <c>{module}.{resource}.{action}</c> in lower case and are
/// constants in the module Contracts; they are never derived from class or method names. A
/// <see cref="PlatformOnly"/> permission (tenant catalog, global configuration) belongs to the platform administrator
/// and cannot be granted to tenant roles.
/// </summary>
public sealed record PermissionDefinition(
    string Key,
    string Module,
    string DisplayKey,
    string? Group = null,
    bool PlatformOnly = false);

/// <summary>Navigation entry declared by a module, filtered by permission on the server.</summary>
public sealed record NavigationDefinition(
    string Key,
    string Module,
    string LabelKey,
    string Path,
    string Icon,
    string? Permission,
    int Order = 0,
    string? Feature = null);

/// <summary>Each module declares its permissions and navigation (discovered by DI, no central list).</summary>
public interface IPermissionProvider
{
    IEnumerable<PermissionDefinition> GetPermissions();

    IEnumerable<NavigationDefinition> GetNavigation() => [];
}

/// <summary>
/// Effective permissions of a user, resolved on the server with HybridCache and invalidated by version.
/// The JWT stays small: it only carries <c>sub</c>, <c>tid</c>, <c>ou</c> and <c>pv</c>.
/// </summary>
public interface IPermissionService
{
    ValueTask<IReadOnlySet<string>> GetPermissionsAsync(CancellationToken ct);

    ValueTask<bool> HasPermissionAsync(string permission, CancellationToken ct);
}

/// <summary>Writes the permissions declared by every module into the database. Runs in the migration service.</summary>
public interface IPermissionSynchronizer
{
    Task SynchronizeAsync(CancellationToken ct);
}

/// <summary>Well-known permission that removes the organization filter.</summary>
public static class PlatformPermissions
{
    public const string OrganizationScopeAll = "organization.scope.all";
}
