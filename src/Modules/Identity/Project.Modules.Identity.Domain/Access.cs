using Project.SharedKernel.Domain;

namespace Project.Modules.Identity.Domain;

/// <summary>
/// Role. <see cref="TenantId"/> null means a platform role shared by every tenant (SuperAdmin and templates);
/// otherwise the role belongs to one tenant.
/// </summary>
public sealed class Role : AuditableAggregateRoot
{
    private readonly List<RolePermission> _permissions = [];

    private Role()
    {
    }

    public string Name { get; private set; } = string.Empty;

    [AuditIgnore]
    public string NormalizedName { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public Guid? TenantId { get; private set; }

    /// <summary>System roles cannot be renamed or deleted.</summary>
    public bool IsSystem { get; private set; }

    public IReadOnlyCollection<RolePermission> Permissions => _permissions;

    public static Role Create(string name, string? description, Guid? tenantId, bool isSystem = false) => new()
    {
        Name = name.Trim(),
        NormalizedName = User.Normalize(name),
        Description = description?.Trim(),
        TenantId = tenantId,
        IsSystem = isSystem,
    };

    public void Update(string name, string? description)
    {
        Name = name.Trim();
        NormalizedName = User.Normalize(name);
        Description = description?.Trim();
    }

    /// <summary>Replaces the permission set. Returns true when it changed.</summary>
    public bool SetPermissions(IEnumerable<string> keys)
    {
        var target = keys.Select(k => k.Trim().ToLowerInvariant()).ToHashSet();
        var current = _permissions.Select(p => p.PermissionKey).ToHashSet();
        if (target.SetEquals(current))
        {
            return false;
        }

        _permissions.RemoveAll(p => !target.Contains(p.PermissionKey));
        foreach (var key in target.Except(current))
        {
            _permissions.Add(new RolePermission(Id, key));
        }

        Raise(new RolePermissionsChanged(Id));
        return true;
    }
}

public sealed class RolePermission
{
    private RolePermission()
    {
    }

    internal RolePermission(Guid roleId, string permissionKey)
    {
        RoleId = roleId;
        PermissionKey = permissionKey;
    }

    public Guid RoleId { get; private set; }

    public string PermissionKey { get; private set; } = string.Empty;
}

/// <summary>Permission declared by a module, synchronized by the migration service.</summary>
public sealed class Permission : AggregateRoot<string>
{
    private Permission()
    {
    }

    public string Module { get; private set; } = string.Empty;

    public string DisplayKey { get; private set; } = string.Empty;

    public string? Group { get; private set; }

    public static Permission Create(string key, string module, string displayKey, string? group) => new()
    {
        Id = key,
        Module = module,
        DisplayKey = displayKey,
        Group = group,
    };

    public void Update(string module, string displayKey, string? group)
    {
        Module = module;
        DisplayKey = displayKey;
        Group = group;
    }
}

/// <summary>A user belongs to a tenant.</summary>
public sealed class TenantMembership : AggregateRoot<Guid>
{
    private TenantMembership()
    {
    }

    public Guid UserId { get; private set; }

    public Guid TenantId { get; private set; }

    public bool IsDefault { get; private set; }

    public DateTimeOffset JoinedAt { get; private set; }

    public static TenantMembership Create(Guid userId, Guid tenantId, bool isDefault, DateTimeOffset now) => new()
    {
        Id = NewId.Next(),
        UserId = userId,
        TenantId = tenantId,
        IsDefault = isDefault,
        JoinedAt = now,
    };
}

/// <summary>
/// Role granted to a user. <see cref="TenantId"/> null grants it on the whole platform;
/// <see cref="OrganizationUnitId"/> restricts it to a unit and its descendants.
/// </summary>
public sealed class RoleAssignment : AggregateRoot<Guid>
{
    private RoleAssignment()
    {
    }

    public Guid UserId { get; private set; }

    public Guid RoleId { get; private set; }

    public Guid? TenantId { get; private set; }

    public Guid? OrganizationUnitId { get; private set; }

    public static RoleAssignment Create(Guid userId, Guid roleId, Guid? tenantId, Guid? organizationUnitId) => new()
    {
        Id = NewId.Next(),
        UserId = userId,
        RoleId = roleId,
        TenantId = tenantId,
        OrganizationUnitId = organizationUnitId,
    };
}

/// <summary>
/// Refresh token (only its SHA-256 hash is stored). Tokens rotate on every use; reusing a revoked token revokes
/// the whole family because it signals theft.
/// </summary>
public sealed class RefreshToken : AggregateRoot<Guid>
{
    private RefreshToken()
    {
    }

    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public Guid FamilyId { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid? OrganizationUnitId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public Guid? ReplacedByTokenId { get; private set; }

    public string? CreatedByIp { get; private set; }

    public static RefreshToken Create(
        Guid userId,
        string tokenHash,
        Guid familyId,
        Guid tenantId,
        Guid? organizationUnitId,
        DateTimeOffset now,
        TimeSpan lifetime,
        string? ip) => new()
    {
        Id = NewId.Next(),
        UserId = userId,
        TokenHash = tokenHash,
        FamilyId = familyId,
        TenantId = tenantId,
        OrganizationUnitId = organizationUnitId,
        CreatedAt = now,
        ExpiresAt = now.Add(lifetime),
        CreatedByIp = ip,
    };

    public bool IsActive(DateTimeOffset now) => RevokedAt is null && ExpiresAt > now;

    public void Revoke(DateTimeOffset now, Guid? replacedBy = null)
    {
        RevokedAt ??= now;
        ReplacedByTokenId ??= replacedBy;
    }
}

/// <summary>Previous password hashes, to prevent reuse.</summary>
public sealed class PasswordHistory : AggregateRoot<Guid>
{
    private PasswordHistory()
    {
    }

    public Guid UserId { get; private set; }

    public string PasswordHash { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public static PasswordHistory Create(Guid userId, string hash, DateTimeOffset now) => new()
    {
        Id = NewId.Next(),
        UserId = userId,
        PasswordHash = hash,
        CreatedAt = now,
    };
}

/// <summary>Single-use password reset token (hash only).</summary>
public sealed class PasswordResetToken : AggregateRoot<Guid>
{
    private PasswordResetToken()
    {
    }

    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? UsedAt { get; private set; }

    public static PasswordResetToken Create(Guid userId, string hash, DateTimeOffset expiresAt) => new()
    {
        Id = NewId.Next(),
        UserId = userId,
        TokenHash = hash,
        ExpiresAt = expiresAt,
    };

    public bool IsValid(DateTimeOffset now) => UsedAt is null && ExpiresAt > now;

    public void MarkUsed(DateTimeOffset now) => UsedAt = now;
}

/// <summary>Sign-in attempt with the real client IP and user agent.</summary>
public sealed class LoginAudit : AggregateRoot<Guid>
{
    private LoginAudit()
    {
    }

    public Guid? UserId { get; private set; }

    public string UserName { get; private set; } = string.Empty;

    public Guid? TenantId { get; private set; }

    public bool Succeeded { get; private set; }

    public string? FailureReason { get; private set; }

    public string? IpAddress { get; private set; }

    public string? UserAgent { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public static LoginAudit Create(
        Guid? userId,
        string userName,
        Guid? tenantId,
        bool succeeded,
        string? failureReason,
        string? ip,
        string? userAgent,
        DateTimeOffset now) => new()
    {
        Id = NewId.Next(),
        UserId = userId,
        UserName = userName.Length > 256 ? userName[..256] : userName,
        TenantId = tenantId,
        Succeeded = succeeded,
        FailureReason = failureReason,
        IpAddress = ip,
        UserAgent = userAgent,
        OccurredAt = now,
    };
}
