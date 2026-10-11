using FluentValidation;
using Microsoft.Extensions.Options;
using Project.Application.Abstractions;
using Project.Application.Messaging;
using Project.Application.Organization;
using Project.Application.Security;
using Project.Application.Settings;
using Project.Application.Tenancy;
using Project.Modules.Identity.Domain;
using Project.SharedKernel;

namespace Project.Modules.Identity.Application.Auth;

// ----- Login -----

/// <summary>Sign in. <see cref="TenantCode"/> is optional: the tenant usually comes from the host.</summary>
public sealed record LoginCommand(string UserName, string Password, string? TenantCode = null) : ICommand<AuthResult>;

public sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(c => c.UserName).NotEmpty().MaximumLength(256);
        RuleFor(c => c.Password).NotEmpty().MaximumLength(256);
    }
}

public sealed class LoginHandler(
    IRepository<User, Guid> users,
    IRepository<LoginAudit, Guid> audits,
    IPasswordHasher hasher,
    SessionIssuer sessions,
    ITenantContext tenantContext,
    ITenantStore tenantStore,
    ISettingsProvider settings,
    ICurrentUser caller,
    TimeProvider clock) : ICommandHandler<LoginCommand, AuthResult>
{
    private static readonly Error InvalidCredentials = Error.Unauthorized("auth.invalid_credentials", "The user name or password is incorrect.");

    public async ValueTask<Result<AuthResult>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var normalized = User.Normalize(command.UserName);
        var user = await users.FirstOrDefaultAsync(
            new Specification<User>().Where(u => u.NormalizedUserName == normalized || u.NormalizedEmail == normalized).Tracked(),
            cancellationToken);

        if (user is null || !user.IsActive)
        {
            // Same work and same answer whether the user exists or not.
            hasher.Verify(command.Password, hasher.TimingSafeDummyHash);
            return await FailAsync(user?.Id, command.UserName, user is null ? "unknown_user" : "inactive", InvalidCredentials, cancellationToken);
        }

        if (user.IsLockedOut(now))
        {
            return await FailAsync(user.Id, command.UserName, "locked_out",
                Error.Unauthorized("auth.locked_out", "The account is temporarily locked. Try again later."), cancellationToken);
        }

        if (!hasher.Verify(command.Password, user.PasswordHash))
        {
            var security = await settings.GetAsync<SecuritySettings>(cancellationToken);
            user.RegisterFailedLogin(security.MaxFailedAccessAttempts, TimeSpan.FromMinutes(security.LockoutMinutes), now);
            return await FailAsync(user.Id, command.UserName, "invalid_password", InvalidCredentials, cancellationToken);
        }

        var requested = tenantContext.Mode != TenancyMode.None && tenantContext.IsResolved
            ? tenantContext.Tenant
            : null;
        if (requested is null && !string.IsNullOrWhiteSpace(command.TenantCode) && tenantContext.Mode != TenancyMode.None)
        {
            requested = await tenantStore.FindByCodeAsync(command.TenantCode, cancellationToken);
            if (requested is null)
            {
                return await FailAsync(user.Id, command.UserName, "unknown_tenant", Error.Forbidden("auth.tenant_access_denied", "You do not have access to this tenant."), cancellationToken);
            }
        }

        var tenant = await sessions.ResolveTenantAsync(user, requested, cancellationToken);
        if (tenant.IsFailure)
        {
            return await FailAsync(user.Id, command.UserName, tenant.Error!.Code, tenant.Error, cancellationToken);
        }

        if (hasher.NeedsRehash(user.PasswordHash))
        {
            user.UpgradePasswordHash(hasher.Hash(command.Password));
        }

        user.RegisterSuccessfulLogin(now);
        var session = await sessions.IssueAsync(user, tenant.Value.Id, organizationUnitId: null, familyId: null, cancellationToken);
        audits.Add(LoginAudit.Create(user.Id, command.UserName, tenant.Value.Id, true, null, caller.IpAddress, caller.UserAgent, now));
        await users.UnitOfWork.SaveChangesAsync(cancellationToken);
        return session;
    }

    private async Task<Result<AuthResult>> FailAsync(Guid? userId, string userName, string reason, Error error, CancellationToken ct)
    {
        audits.Add(LoginAudit.Create(userId, userName, tenantContext.TenantId, false, reason, caller.IpAddress, caller.UserAgent, clock.GetUtcNow()));
        await users.UnitOfWork.SaveChangesAsync(ct);
        return error;
    }
}

// ----- Refresh -----

/// <summary>Rotates the refresh token. Reusing a revoked token revokes the whole family.</summary>
public sealed record RefreshSessionCommand(string RefreshToken) : ICommand<AuthResult>;

public sealed class RefreshSessionHandler(
    IRepository<RefreshToken, Guid> refreshTokens,
    IRepository<User, Guid> users,
    ISecureTokenGenerator generator,
    SessionIssuer sessions,
    ITenantStore tenantStore,
    TimeProvider clock) : ICommandHandler<RefreshSessionCommand, AuthResult>
{
    private static readonly Error Invalid = Error.Unauthorized("auth.invalid_refresh_token", "The session expired. Sign in again.");

    public async ValueTask<Result<AuthResult>> Handle(RefreshSessionCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.RefreshToken))
        {
            return Invalid;
        }

        var now = clock.GetUtcNow();
        var hash = generator.Hash(command.RefreshToken);
        var token = await refreshTokens.FirstOrDefaultAsync(new Specification<RefreshToken>().Where(t => t.TokenHash == hash).Tracked(), cancellationToken);
        if (token is null)
        {
            return Invalid;
        }

        if (token.RevokedAt is not null)
        {
            // Reuse of a rotated token: assume theft and end the whole family.
            var family = await refreshTokens.ListAsync(
                new Specification<RefreshToken>().Where(t => t.FamilyId == token.FamilyId && t.RevokedAt == null).Tracked(),
                cancellationToken);
            foreach (var member in family)
            {
                member.Revoke(now);
            }

            await refreshTokens.UnitOfWork.SaveChangesAsync(cancellationToken);
            return Error.Unauthorized("auth.refresh_token_reused", "The session was revoked for security reasons. Sign in again.");
        }

        if (!token.IsActive(now))
        {
            return Invalid;
        }

        var user = await users.FirstOrDefaultAsync(new Specification<User>().Where(u => u.Id == token.UserId).Tracked(), cancellationToken);
        if (user is null || !user.IsActive || user.IsLockedOut(now))
        {
            token.Revoke(now);
            await refreshTokens.UnitOfWork.SaveChangesAsync(cancellationToken);
            return Invalid;
        }

        var tenant = await sessions.ResolveTenantAsync(user, await tenantStore.FindByIdAsync(token.TenantId, cancellationToken), cancellationToken);
        if (tenant.IsFailure)
        {
            token.Revoke(now);
            await refreshTokens.UnitOfWork.SaveChangesAsync(cancellationToken);
            return tenant.Error!;
        }

        var (session, replacementId) = await sessions.IssueWithIdAsync(user, token.TenantId, token.OrganizationUnitId, token.FamilyId, cancellationToken);
        token.Revoke(now, replacementId);
        await refreshTokens.UnitOfWork.SaveChangesAsync(cancellationToken);
        return session;
    }
}

// ----- Logout -----

public sealed record LogoutCommand(string? RefreshToken) : ICommand;

public sealed class LogoutHandler(IRepository<RefreshToken, Guid> refreshTokens, ISecureTokenGenerator generator, TimeProvider clock)
    : ICommandHandler<LogoutCommand>
{
    public async ValueTask<Result> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.RefreshToken))
        {
            return Result.Success();
        }

        var hash = generator.Hash(command.RefreshToken);
        var token = await refreshTokens.FirstOrDefaultAsync(new Specification<RefreshToken>().Where(t => t.TokenHash == hash).Tracked(), cancellationToken);
        if (token is not null)
        {
            token.Revoke(clock.GetUtcNow());
            await refreshTokens.UnitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}

// ----- Change password (signed in) -----

public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword) : ICommand<AuthResult>;

public sealed class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordValidator()
    {
        RuleFor(c => c.CurrentPassword).NotEmpty();
        RuleFor(c => c.NewPassword).NotEmpty().MaximumLength(128).NotEqual(c => c.CurrentPassword)
            .WithMessage("The new password must be different from the current one.");
    }
}

public sealed class ChangePasswordHandler(
    IRepository<User, Guid> users,
    IPasswordHasher hasher,
    PasswordChanger changer,
    SessionIssuer sessions,
    ICurrentUser currentUser,
    ITenantContext tenant) : ICommandHandler<ChangePasswordCommand, AuthResult>
{
    public async ValueTask<Result<AuthResult>> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(currentUser.UserId, out var userId))
        {
            return Error.Unauthorized("auth.required", "Authentication is required.");
        }

        var user = await users.FirstOrDefaultAsync(new Specification<User>().Where(u => u.Id == userId).Tracked(), cancellationToken);
        if (user is null)
        {
            return Error.Unauthorized("auth.required", "Authentication is required.");
        }

        if (!hasher.Verify(command.CurrentPassword, user.PasswordHash))
        {
            return Error.Validation("currentPassword", "The current password is incorrect.");
        }

        var failed = await changer.ChangeAsync(user, command.NewPassword, mustChangePassword: false, cancellationToken);
        if (failed is not null)
        {
            return failed;
        }

        var session = await sessions.IssueAsync(user, tenant.TenantId ?? TenantDefaults.DefaultTenantId, currentUser.ActiveOrganizationUnitId, null, cancellationToken);
        await users.UnitOfWork.SaveChangesAsync(cancellationToken);
        return session;
    }
}

// ----- Forgot / reset password -----

public sealed record ForgotPasswordCommand(string Email) : ICommand;

public sealed class ForgotPasswordValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordValidator() => RuleFor(c => c.Email).NotEmpty().EmailAddress().MaximumLength(256);
}

/// <summary>Always succeeds so the endpoint cannot be used to discover accounts.</summary>
public sealed class ForgotPasswordHandler(
    IRepository<User, Guid> users,
    IRepository<PasswordResetToken, Guid> resetTokens,
    ISecureTokenGenerator generator,
    IEmailSender email,
    IOptions<IdentityOptions> options,
    TimeProvider clock) : ICommandHandler<ForgotPasswordCommand>
{
    public async ValueTask<Result> Handle(ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        var normalized = User.Normalize(command.Email);
        var user = await users.FirstOrDefaultAsync(new Specification<User>().Where(u => u.NormalizedEmail == normalized && u.IsActive), cancellationToken);
        if (user is null)
        {
            return Result.Success();
        }

        var (token, hash) = generator.Generate();
        resetTokens.Add(PasswordResetToken.Create(user.Id, hash, clock.GetUtcNow().AddMinutes(options.Value.PasswordResetMinutes)));
        await resetTokens.UnitOfWork.SaveChangesAsync(cancellationToken);

        var link = $"{options.Value.PublicUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(token)}";
        await email.SendAsync(
            user.Email,
            "Password reset",
            $"<p>Use this link to choose a new password. It expires in {options.Value.PasswordResetMinutes} minutes.</p><p><a href=\"{link}\">{link}</a></p>",
            cancellationToken);
        return Result.Success();
    }
}

public sealed record ResetPasswordCommand(string Token, string NewPassword) : ICommand;

public sealed class ResetPasswordValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordValidator()
    {
        RuleFor(c => c.Token).NotEmpty().MaximumLength(512);
        RuleFor(c => c.NewPassword).NotEmpty().MaximumLength(128);
    }
}

public sealed class ResetPasswordHandler(
    IRepository<PasswordResetToken, Guid> resetTokens,
    IRepository<User, Guid> users,
    ISecureTokenGenerator generator,
    PasswordChanger changer,
    TimeProvider clock) : ICommandHandler<ResetPasswordCommand>
{
    public async ValueTask<Result> Handle(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var hash = generator.Hash(command.Token);
        var token = await resetTokens.FirstOrDefaultAsync(new Specification<PasswordResetToken>().Where(t => t.TokenHash == hash).Tracked(), cancellationToken);
        if (token is null || !token.IsValid(now))
        {
            return Error.Validation("token", "The reset link is invalid or expired.");
        }

        var user = await users.FirstOrDefaultAsync(new Specification<User>().Where(u => u.Id == token.UserId).Tracked(), cancellationToken);
        if (user is null || !user.IsActive)
        {
            return Error.Validation("token", "The reset link is invalid or expired.");
        }

        var failed = await changer.ChangeAsync(user, command.NewPassword, mustChangePassword: false, cancellationToken);
        if (failed is not null)
        {
            return failed;
        }

        user.Unlock();
        token.MarkUsed(now);
        await users.UnitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

// ----- Switch tenant / organization unit -----

/// <summary>
/// Issues a new session for another tenant (the unit is cleared) or for another unit of the current tenant.
/// </summary>
public sealed record SwitchContextCommand(Guid TenantId, Guid? OrganizationUnitId) : ICommand<AuthResult>;

public sealed class SwitchContextHandler(
    IRepository<User, Guid> users,
    SessionIssuer sessions,
    ITenantStore tenantStore,
    ITenantContext tenantContext,
    ICurrentUser currentUser,
    IOrganizationContext organization,
    IOrganizationDirectory directory,
    IPermissionService permissions) : ICommandHandler<SwitchContextCommand, AuthResult>
{
    public async ValueTask<Result<AuthResult>> Handle(SwitchContextCommand command, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(currentUser.UserId, out var userId))
        {
            return Error.Unauthorized("auth.required", "Authentication is required.");
        }

        var user = await users.FirstOrDefaultAsync(new Specification<User>().Where(u => u.Id == userId && u.IsActive).Tracked(), cancellationToken);
        if (user is null)
        {
            return Error.Unauthorized("auth.required", "Authentication is required.");
        }

        var target = await tenantStore.FindByIdAsync(command.TenantId, cancellationToken);
        var tenant = await sessions.ResolveTenantAsync(user, target ?? new TenantInfo(command.TenantId, string.Empty, string.Empty, TenantStatus.Archived, TenantIsolation.Shared, null, null, null, []), cancellationToken);
        if (tenant.IsFailure)
        {
            return tenant.Error!;
        }

        Guid? unit = null;
        if (command.OrganizationUnitId is { } requestedUnit)
        {
            if (tenant.Value.Id != tenantContext.TenantId)
            {
                return Error.Validation("organizationUnitId", "Switch the tenant first, then the organization unit.");
            }

            if (!organization.Enabled)
            {
                return Error.Validation("organizationUnitId", "Organization units are not enabled.");
            }

            var path = await directory.GetPathAsync(requestedUnit, cancellationToken);
            if (path.Count == 0)
            {
                return Error.NotFound("organization.unit_not_found");
            }

            var fullAccess = await permissions.HasPermissionAsync(PlatformPermissions.OrganizationScopeAll, cancellationToken);
            if (!fullAccess)
            {
                var assigned = await directory.GetAssignedUnitsAsync(user.Id.ToString(), cancellationToken);
                if (!path.Any(assigned.Contains))
                {
                    return Error.Forbidden("organization.unit_forbidden", "You are not assigned to this organization unit.");
                }
            }

            unit = requestedUnit;
        }

        var session = await sessions.IssueAsync(user, tenant.Value.Id, unit, null, cancellationToken);
        await users.UnitOfWork.SaveChangesAsync(cancellationToken);
        return session;
    }
}
