using Mediator;
using Project.Application.Abstractions;
using Project.SharedKernel;

namespace Project.Modules.Identity.Application.Auth;

public sealed record ChangePasswordCommand(Guid UserId, string CurrentPassword, string NewPassword)
    : Project.Application.Mediator.ICommand<Result>;

/// <summary>
/// Change password handler. Verifies current password, validates the new one
/// against <see cref="IPasswordPolicy"/> and <see cref="IPasswordHistoryChecker"/>,
/// updates the BCrypt hash and resets <c>FailedLoginAttempts</c>.
/// </summary>
public sealed class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result>
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _hasher;
    private readonly IPasswordPolicy _policy;
    private readonly IPasswordHistoryChecker _history;

    public ChangePasswordCommandHandler(
        IUnitOfWork uow,
        IPasswordHasher hasher,
        IPasswordPolicy policy,
        IPasswordHistoryChecker history)
    {
        _uow = uow;
        _hasher = hasher;
        _policy = policy;
        _history = history;
    }

    public async ValueTask<Result> Handle(ChangePasswordCommand command, CancellationToken ct)
    {
        var policy = _policy.Validate(command.NewPassword);
        if (!policy.IsValid)
        {
            return Result.Failure(Error.Validation(
                "identity.password_policy",
                "The new password does not satisfy the password policy.",
                policy.Errors));
        }

        if (await _history.HasBeenUsedRecentlyAsync(command.UserId, command.NewPassword, limit: 5, ct))
        {
            return Result.Failure(Error.Validation(
                "identity.password_reused",
                "The new password matches one of the last 5 passwords."));
        }

        return Result.Failure(
            Error.Failure("identity.not_implemented", "User lookup lands with the IdentityDbContext."));
    }
}