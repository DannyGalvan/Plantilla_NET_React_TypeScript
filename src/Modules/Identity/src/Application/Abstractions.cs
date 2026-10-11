namespace Project.Modules.Identity.Application;

/// <summary>
/// Issues short-lived access tokens. The signing key comes from
/// \`AppSettings:Secret\` in the legacy module; in the new module it is
/// loaded from \`Jwt:SigningKey\` via the Options pattern.
/// </summary>
public interface ITokenIssuer
{
    /// <summary>Returns the signed JWT and its absolute expiration.</summary>
    Task<IssuedToken> IssueAsync(TokenDescriptor descriptor, CancellationToken ct);
}

public sealed record TokenDescriptor(
    Guid UserId,
    string UserName,
    string? TenantId,
    string? OrganizationUnitId,
    long PermissionsVersion,
    IReadOnlyCollection<string> Roles,
    string? Ip);

public sealed record IssuedToken(string Token, DateTimeOffset ExpiresAt, DateTimeOffset NotBefore);

/// <summary>
/// Hashes refresh tokens with SHA-256, rotates them on every refresh, and
/// revokes the entire chain on reuse detection.
/// </summary>
public interface IRefreshTokenService
{
    /// <summary>Generates a (new plaintext, hash) pair for storage.</summary>
    (string PlainToken, string Hash) Generate();

    /// <summary>Hashes the presented plaintext for lookup.</summary>
    string Hash(string plainToken);

    /// <summary>Returns true when the token is expired or already revoked.</summary>
    bool IsExpired(DateTimeOffset expiresAt, DateTimeOffset? revokedAt);
}

/// <summary>
/// BCrypt wrapper. NeedsRehash signals that the cost factor should be bumped.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string hashed, string provided);
    bool NeedsRehash(string hashed);
}

public interface IPasswordPolicy
{
    PasswordPolicyResult Validate(string password);
}

public sealed record PasswordPolicyResult(bool IsValid, IReadOnlyDictionary<string, string[]>? Errors)
{
    public static PasswordPolicyResult Success { get; } = new(true, null);
}

/// <summary>
/// Ensures the new password has not been used recently (PasswordHistoryLimit).
/// </summary>
public interface IPasswordHistoryChecker
{
    Task<bool> HasBeenUsedRecentlyAsync(Guid userId, string newPassword, int limit, CancellationToken ct);
}

public interface ILockoutPolicy
{
    bool IsLockedOut(int failedAttempts, DateTimeOffset? lockoutEnd, DateTimeOffset now);
    DateTimeOffset ComputeLockoutEnd(int failedAttempts, DateTimeOffset now);
    int IncrementFailedAttempts(int current);
    int ResetFailedAttempts();
}

public interface ILoginAuditWriter
{
    Task WriteAsync(LoginAuditDescriptor descriptor, CancellationToken ct);
}

public sealed record LoginAuditDescriptor(
    Guid UserId,
    string? UserName,
    bool Succeeded,
    string? Ip,
    string? UserAgent,
    string? Reason,
    Guid? TenantId);

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body, CancellationToken ct);
}

/// <summary>
/// Resolves the user's effective permissions. Backed by HybridCache keyed by
/// \`perm:{tid}:{userId}:{pv}\` so changes to permissions propagate without
/// re-login (PermissionsVersion changes on RolePermissionsChanged).
/// </summary>
public interface IPermissionService
{
    ValueTask<IReadOnlySet<string>> GetEffectivePermissionsAsync(
        Guid? tenantId, Guid userId, long permissionsVersion, Guid? activeOrganizationUnitId, CancellationToken ct);

    ValueTask<bool> HasAsync(
        Guid? tenantId, Guid userId, long permissionsVersion, Guid? activeOrganizationUnitId, string permission, CancellationToken ct);

    Task InvalidateAsync(Guid userId, CancellationToken ct);
}