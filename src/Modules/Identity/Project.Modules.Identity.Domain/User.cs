using Project.SharedKernel.Domain;

namespace Project.Modules.Identity.Domain;

/// <summary>
/// Global user (platform database). A user may belong to several tenants through <see cref="TenantMembership"/>.
/// </summary>
public sealed class User : AuditableAggregateRoot
{
    private User()
    {
    }

    public string UserName { get; private set; } = string.Empty;

    [AuditIgnore]
    public string NormalizedUserName { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    [AuditIgnore]
    public string NormalizedEmail { get; private set; } = string.Empty;

    public string? DisplayName { get; private set; }

    public string PasswordHash { get; private set; } = string.Empty;

    /// <summary>Changes on every credential change; used to invalidate outstanding sessions.</summary>
    [AuditIgnore]
    public string SecurityStamp { get; private set; } = Guid.NewGuid().ToString("N");

    /// <summary>Incremented when the user's effective permissions change; travels in the <c>pv</c> claim.</summary>
    [AuditIgnore]
    public long PermissionsVersion { get; private set; } = 1;

    [AuditIgnore]
    public int AccessFailedCount { get; private set; }

    [AuditIgnore]
    public DateTimeOffset? LockoutEnd { get; private set; }

    public bool MustChangePassword { get; private set; }

    public bool IsActive { get; private set; } = true;

    [AuditIgnore]
    public DateTimeOffset? LastLoginAt { get; private set; }

    public DateTimeOffset? PasswordChangedAt { get; private set; }

    public static string Normalize(string value) => value.Trim().ToUpperInvariant();

    public static User Create(string userName, string email, string? displayName, string passwordHash, bool mustChangePassword, DateTimeOffset now)
    {
        var user = new User
        {
            UserName = userName.Trim(),
            NormalizedUserName = Normalize(userName),
            Email = email.Trim(),
            NormalizedEmail = Normalize(email),
            DisplayName = displayName?.Trim(),
            PasswordHash = passwordHash,
            MustChangePassword = mustChangePassword,
            PasswordChangedAt = now,
        };
        user.Raise(new UserCreated(user.Id, user.UserName));
        return user;
    }

    public void UpdateProfile(string email, string? displayName)
    {
        Email = email.Trim();
        NormalizedEmail = Normalize(email);
        DisplayName = displayName?.Trim();
    }

    public void SetActive(bool active)
    {
        if (IsActive == active)
        {
            return;
        }

        IsActive = active;
        RenewSecurityStamp();
        BumpPermissionsVersion();
    }

    public bool IsLockedOut(DateTimeOffset now) => LockoutEnd is { } end && end > now;

    /// <summary>Registers a failed sign-in. Returns true when the account becomes locked.</summary>
    public bool RegisterFailedLogin(int maxAttempts, TimeSpan lockout, DateTimeOffset now)
    {
        AccessFailedCount++;
        if (AccessFailedCount < maxAttempts)
        {
            return false;
        }

        AccessFailedCount = 0;
        LockoutEnd = now.Add(lockout);
        Raise(new UserLockedOut(Id, LockoutEnd.Value));
        return true;
    }

    public void RegisterSuccessfulLogin(DateTimeOffset now)
    {
        AccessFailedCount = 0;
        LockoutEnd = null;
        LastLoginAt = now;
    }

    public void Unlock()
    {
        AccessFailedCount = 0;
        LockoutEnd = null;
    }

    public void ChangePassword(string passwordHash, bool mustChangePassword, DateTimeOffset now)
    {
        PasswordHash = passwordHash;
        MustChangePassword = mustChangePassword;
        PasswordChangedAt = now;
        RenewSecurityStamp();
        Raise(new PasswordChanged(Id));
    }

    /// <summary>Upgrades the hash (for example after raising the BCrypt work factor) without other side effects.</summary>
    public void UpgradePasswordHash(string passwordHash) => PasswordHash = passwordHash;

    public void BumpPermissionsVersion() => PermissionsVersion++;

    private void RenewSecurityStamp() => SecurityStamp = Guid.NewGuid().ToString("N");
}

public sealed record UserCreated(Guid UserId, string UserName) : IDomainEvent;

public sealed record PasswordChanged(Guid UserId) : IDomainEvent;

public sealed record UserLockedOut(Guid UserId, DateTimeOffset LockoutEnd) : IDomainEvent;

public sealed record RolePermissionsChanged(Guid RoleId) : IDomainEvent;
