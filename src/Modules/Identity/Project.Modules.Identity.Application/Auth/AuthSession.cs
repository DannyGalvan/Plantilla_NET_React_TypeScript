using Project.Application.Abstractions;
using Project.Application.Settings;
using Project.Application.Tenancy;
using Project.Modules.Identity.Domain;
using Project.SharedKernel;

namespace Project.Modules.Identity.Application.Auth;

/// <summary>Tokens returned after sign-in, refresh, password change or context switch.</summary>
public sealed record AuthResult(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    bool MustChangePassword,
    Guid TenantId,
    Guid? OrganizationUnitId);

/// <summary>Creates sessions (access + refresh token) and validates tenant access. Shared by the auth use cases.</summary>
public sealed class SessionIssuer(
    IRepository<RefreshToken, Guid> refreshTokens,
    IRepository<TenantMembership, Guid> memberships,
    IIdentityReadService reads,
    ITokenService tokens,
    ISecureTokenGenerator generator,
    ITenantStore tenantStore,
    ICurrentUser currentUser,
    TimeProvider clock)
{
    /// <summary>Adds a refresh token to the unit of work and returns the session. The caller saves.</summary>
    public async Task<AuthResult> IssueAsync(User user, Guid tenantId, Guid? organizationUnitId, Guid? familyId, CancellationToken ct) =>
        (await IssueWithIdAsync(user, tenantId, organizationUnitId, familyId, ct)).Session;

    /// <summary>Same as <see cref="IssueAsync"/> and also returns the id of the new refresh token.</summary>
    public async Task<(AuthResult Session, Guid RefreshTokenId)> IssueWithIdAsync(
        User user,
        Guid tenantId,
        Guid? organizationUnitId,
        Guid? familyId,
        CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var (plain, hash) = generator.Generate();
        var refresh = RefreshToken.Create(user.Id, hash, familyId ?? Guid.NewGuid(), tenantId, organizationUnitId, now, tokens.RefreshTokenLifetime, currentUser.IpAddress);
        refreshTokens.Add(refresh);

        var roles = await reads.GetRoleNamesAsync(user.Id, tenantId, ct);
        var access = tokens.CreateAccessToken(new AccessTokenRequest(user.Id, user.UserName, user.Email, tenantId, organizationUnitId, user.PermissionsVersion, roles));
        return (new AuthResult(access.Token, access.ExpiresAt, plain, refresh.ExpiresAt, user.MustChangePassword, tenantId, organizationUnitId), refresh.Id);
    }

    /// <summary>
    /// Picks the tenant of the session: the requested one (host, header or explicit), or the user's default membership.
    /// Platform administrators may enter any active tenant.
    /// </summary>
    public async Task<Result<TenantInfo>> ResolveTenantAsync(User user, TenantInfo? requested, CancellationToken ct)
    {
        var userMemberships = await memberships.ListAsync(new Specification<TenantMembership>().Where(m => m.UserId == user.Id), ct);
        var isPlatformAdmin = await reads.IsPlatformAdminAsync(user.Id, ct);

        TenantInfo? tenant;
        if (requested is not null)
        {
            if (!isPlatformAdmin && userMemberships.All(m => m.TenantId != requested.Id))
            {
                return Error.Forbidden("auth.tenant_access_denied", "You do not have access to this tenant.");
            }

            tenant = requested;
        }
        else
        {
            var membership = userMemberships.OrderByDescending(m => m.IsDefault).ThenBy(m => m.JoinedAt).FirstOrDefault();
            var tenantId = membership?.TenantId ?? (isPlatformAdmin ? TenantDefaults.DefaultTenantId : (Guid?)null);
            if (tenantId is null)
            {
                return Error.Forbidden("auth.no_tenant", "Your account is not assigned to any tenant.");
            }

            tenant = await tenantStore.FindByIdAsync(tenantId.Value, ct);
        }

        if (tenant is null)
        {
            return Error.Forbidden("auth.tenant_access_denied", "You do not have access to this tenant.");
        }

        return tenant.Status == TenantStatus.Active
            ? tenant
            : Error.Forbidden("tenant.suspended", "The tenant is not active.");
    }
}

/// <summary>Password rules from the cascading <see cref="SecuritySettings"/>.</summary>
public sealed class PasswordPolicy(ISettingsProvider settings)
{
    public async Task<Error?> ValidateAsync(string password, string? userName, CancellationToken ct)
    {
        var security = await settings.GetAsync<SecuritySettings>(ct);
        var problems = new List<string>();

        if (password.Length < security.PasswordMinLength)
        {
            problems.Add($"Use at least {security.PasswordMinLength} characters.");
        }

        if (!password.Any(char.IsUpper) || !password.Any(char.IsLower))
        {
            problems.Add("Use upper and lower case letters.");
        }

        if (!password.Any(char.IsDigit))
        {
            problems.Add("Use at least one digit.");
        }

        if (password.All(char.IsLetterOrDigit))
        {
            problems.Add("Use at least one symbol.");
        }

        if (!string.IsNullOrWhiteSpace(userName) && password.Contains(userName, StringComparison.OrdinalIgnoreCase))
        {
            problems.Add("The password cannot contain the user name.");
        }

        return problems.Count == 0
            ? null
            : Error.Validation("auth.weak_password", "The password does not meet the policy.", new Dictionary<string, string[]> { ["password"] = [.. problems] });
    }
}

/// <summary>Applies a new password: policy, history (no reuse of the last N), hash and history entry.</summary>
public sealed class PasswordChanger(
    PasswordPolicy policy,
    IPasswordHasher hasher,
    IRepository<PasswordHistory, Guid> history,
    IRepository<RefreshToken, Guid> refreshTokens,
    ISettingsProvider settings,
    TimeProvider clock)
{
    public async Task<Error?> ChangeAsync(User user, string newPassword, bool mustChangePassword, CancellationToken ct)
    {
        var weak = await policy.ValidateAsync(newPassword, user.UserName, ct);
        if (weak is not null)
        {
            return weak;
        }

        var security = await settings.GetAsync<SecuritySettings>(ct);
        var previous = await history.ListAsync(
            new Specification<PasswordHistory>().Where(h => h.UserId == user.Id).OrderBy(h => h.CreatedAt, descending: true).Page(0, Math.Max(1, security.PasswordHistoryLimit)),
            ct);

        if (hasher.Verify(newPassword, user.PasswordHash) || previous.Any(p => hasher.Verify(newPassword, p.PasswordHash)))
        {
            return Error.Validation("password", $"The password was used recently. Choose one different from the last {security.PasswordHistoryLimit}.");
        }

        var now = clock.GetUtcNow();
        history.Add(PasswordHistory.Create(user.Id, user.PasswordHash, now));
        user.ChangePassword(hasher.Hash(newPassword), mustChangePassword, now);

        // A credential change ends every other session.
        var active = await refreshTokens.ListAsync(
            new Specification<RefreshToken>().Where(t => t.UserId == user.Id && t.RevokedAt == null).Tracked(),
            ct);
        foreach (var token in active)
        {
            token.Revoke(now);
        }

        return null;
    }
}
