using Mediator;
using Project.Application.Abstractions;
using Project.SharedKernel;

namespace Project.Modules.Identity.Application.Auth;

public sealed record LoginCommand(string UserName, string Password, string? Ip, string? UserAgent)
    : Project.Application.Mediator.ICommand<AuthResponse>;

public sealed record AuthResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    UserInfo User);

public sealed record UserInfo(
    Guid Id,
    string UserName,
    string Email,
    long PermissionsVersion,
    Guid? TenantId,
    IReadOnlyCollection<string> Roles,
    bool MustChangePassword);

/// <summary>
/// Login handler. Single SaveChanges round; expected failures return
/// <see cref="Error.Unauthorized"/> or <see cref="Error.Failure"/> without
/// disclosing whether the user exists.
/// </summary>
public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResponse>>
{
    private readonly IUnitOfWork _uow;
    private readonly ITokenIssuer _issuer;
    private readonly IRefreshTokenService _refreshTokens;
    private readonly IPasswordHasher _hasher;
    private readonly ILockoutPolicy _lockout;
    private readonly ILoginAuditWriter _audit;

    public LoginCommandHandler(
        IUnitOfWork uow,
        ITokenIssuer issuer,
        IRefreshTokenService refreshTokens,
        IPasswordHasher hasher,
        ILockoutPolicy lockout,
        ILoginAuditWriter audit)
    {
        _uow = uow;
        _issuer = issuer;
        _refreshTokens = refreshTokens;
        _hasher = hasher;
        _lockout = lockout;
        _audit = audit;
    }

    public ValueTask<Result<AuthResponse>> Handle(LoginCommand command, CancellationToken ct)
    {
        // Phase 4 skeleton: IdentityDbContext + User lookup live in the next
        // slice of Fase 2. The handler returns 501 until then.
        _audit.WriteAsync(new LoginAuditDescriptor(
            UserId: Guid.Empty,
            UserName: command.UserName,
            Succeeded: false,
            Ip: command.Ip,
            UserAgent: command.UserAgent,
            Reason: "Phase 4 skeleton",
            TenantId: null),
            ct).GetAwaiter().GetResult();

        return ValueTask.FromResult(Result<AuthResponse>.Failure(
            Error.Failure("identity.not_implemented", "Identity module DbContext lands with the next slice of Fase 2.")));
    }
}