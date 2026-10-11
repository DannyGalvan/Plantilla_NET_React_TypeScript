using Project.SharedKernel.Domain;

namespace Project.Modules.Identity.Domain;

/// <summary>
/// Identity aggregate. Carries lockout, security stamp and permissions
/// version (incremented on RolePermissionsChanged to invalidate caches).
/// </summary>
public sealed class User : AggregateRoot<Guid>
{
    public required string UserName { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public string? SecurityStamp { get; set; }
    public string? RecoveryToken { get; set; }
    public DateTimeOffset? RecoveryTokenExpiresAt { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public bool MustChangePassword { get; set; }
    public DateTimeOffset? LastPasswordChange { get; set; }
    public long PermissionsVersion { get; set; }
    public Guid TenantId { get; set; }
    public long? RoleId { get; set; }
}

public sealed class Role : AggregateRoot<Guid>
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public Guid TenantId { get; set; }
}

public sealed class Permission : Entity<long>
{
    public required string Key { get; set; }
    public required string Module { get; set; }
    public string? DisplayKey { get; set; }
    public string? NavKey { get; set; }
    public string? Icon { get; set; }
    public int Order { get; set; }
}

public sealed class RolePermission : Entity<long>
{
    public long RoleId { get; set; }
    public long PermissionId { get; set; }
}

public sealed class RefreshToken : AggregateRoot<Guid>
{
    public Guid UserId { get; set; }
    public required string TokenHash { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? RevokedByIp { get; set; }
    public string? ReplacedByHash { get; set; }
    public Guid TenantId { get; set; }
}

public sealed class LoginAudit : AggregateRoot<long>
{
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public bool Succeeded { get; set; }
    public string? Ip { get; set; }
    public string? UserAgent { get; set; }
    public string? Reason { get; set; }
    public Guid TenantId { get; set; }
}

public sealed class PasswordHistory : Entity<long>
{
    public Guid UserId { get; set; }
    public required string PasswordHash { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}