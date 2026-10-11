using Mediator;
using Project.Application.Abstractions;
using Project.SharedKernel;

namespace Project.Modules.Identity.Application.Auth;

public sealed record RefreshTokenCommand(string RefreshToken, string? Ip, string? UserAgent)
    : Project.Application.Mediator.ICommand<AuthResponse>;

/// <summary>
/// Refresh handler. Hashes the presented token, looks up the active refresh
/// row, rotates the chain (revokes the old and inserts a new one) and issues
/// a fresh access token. Reuse of a revoked token revokes the entire chain.
/// </summary>
public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResponse>>
{
    private readonly IUnitOfWork _uow;
    private readonly IRefreshTokenService _refreshTokens;
    private readonly ITokenIssuer _issuer;
    private readonly IPermissionService _permissions;

    public RefreshTokenCommandHandler(
        IUnitOfWork uow,
        IRefreshTokenService refreshTokens,
        ITokenIssuer issuer,
        IPermissionService permissions)
    {
        _uow = uow;
        _refreshTokens = refreshTokens;
        _issuer = issuer;
        _permissions = permissions;
    }

    public ValueTask<Result<AuthResponse>> Handle(RefreshTokenCommand command, CancellationToken ct)
    {
        return ValueTask.FromResult(Result<AuthResponse>.Failure(
            Error.Failure("identity.not_implemented", "Refresh rotation lands with the IdentityDbContext.")));
    }
}