using Project.Application;
using Project.SharedKernel;

namespace Project.Modules.Identity.Application;

/// <summary>Adaptive password hashing (BCrypt in infrastructure).</summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string hash);

    bool NeedsRehash(string hash);

    /// <summary>Valid hash of a random value, verified when the user does not exist so timing does not reveal it.</summary>
    string TimingSafeDummyHash { get; }
}

public sealed record AccessTokenRequest(
    Guid UserId,
    string UserName,
    string? Email,
    Guid TenantId,
    Guid? OrganizationUnitId,
    long PermissionsVersion,
    IReadOnlyCollection<string> Roles);

public sealed record AccessToken(string Token, DateTimeOffset ExpiresAt);

/// <summary>Issues signed access tokens. The JWT only carries identity and context, never permissions.</summary>
public interface ITokenService
{
    TimeSpan RefreshTokenLifetime { get; }

    AccessToken CreateAccessToken(AccessTokenRequest request);
}

/// <summary>Random opaque tokens (refresh, password reset) stored as SHA-256 hashes.</summary>
public interface ISecureTokenGenerator
{
    (string Token, string Hash) Generate();

    string Hash(string token);
}

/// <summary>Removes cached permissions so changes apply without signing in again.</summary>
public interface IPermissionCache
{
    Task InvalidateUsersAsync(IEnumerable<Guid> userIds, CancellationToken ct);

    Task InvalidateRoleAsync(Guid roleId, CancellationToken ct);
}

/// <summary><c>Identity</c> configuration section.</summary>
public sealed class IdentityOptions
{
    public const string SectionName = "Identity";

    /// <summary>Public URL of the SPA, used to build password reset links.</summary>
    public string PublicUrl { get; set; } = "https://localhost:5173";

    public int PasswordResetMinutes { get; set; } = 30;
}

public sealed record RoleRefDto(Guid Id, string Name);

public sealed record RoleAssignmentDto(Guid Id, Guid RoleId, string RoleName, Guid? OrganizationUnitId);

public sealed record UserDto(
    Guid Id,
    string UserName,
    string Email,
    string? DisplayName,
    bool IsActive,
    bool IsLockedOut,
    bool MustChangePassword,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset CreatedAt,
    IReadOnlyList<RoleRefDto> Roles,
    IReadOnlyList<RoleAssignmentDto> Assignments,
    Guid ConcurrencyStamp);

public sealed record RoleDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsSystem,
    bool IsPlatform,
    IReadOnlyList<string> Permissions,
    int UserCount,
    DateTimeOffset CreatedAt,
    Guid ConcurrencyStamp);

/// <summary>Read side of the module (CQRS): projections optimized for lists and detail screens.</summary>
public interface IIdentityReadService
{
    Task<Result<PagedResult<UserDto>>> ListUsersAsync(PageRequest page, Guid tenantId, CancellationToken ct);

    Task<UserDto?> GetUserAsync(Guid id, Guid tenantId, CancellationToken ct);

    Task<Result<PagedResult<RoleDto>>> ListRolesAsync(PageRequest page, Guid tenantId, CancellationToken ct);

    Task<RoleDto?> GetRoleAsync(Guid id, Guid tenantId, CancellationToken ct);

    /// <summary>Names of the roles the user has in the tenant (tenant-wide and platform assignments).</summary>
    Task<IReadOnlyList<string>> GetRoleNamesAsync(Guid userId, Guid tenantId, CancellationToken ct);

    /// <summary>True when the user has a platform-wide role assignment (no tenant).</summary>
    Task<bool> IsPlatformAdminAsync(Guid userId, CancellationToken ct);
}
