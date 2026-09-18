using FluentValidation.Results;
using Project.Server.Entities.Models;
using Project.Server.Entities.Request;
using Project.Server.Entities.Response;

namespace Project.Server.Services.Interfaces
{
    /// <summary>
    /// Phase 3.8: AuthAsync returns a richer envelope (access + refresh cookie).
    /// </summary>
    public interface IAuthService
    {
        Task<Response<AuthWithRefreshResponse, List<ValidationFailure>>> AuthAsync(LoginRequest model, CancellationToken ct = default);
        Task<Response<User, List<ValidationFailure>>> RegisterAsync(RegisterRequest model, CancellationToken ct = default);
        Task<Response<string, List<ValidationFailure>>> ChangePasswordAsync(ChangePasswordRequest model, CancellationToken ct = default);
        Task<Response<string, List<ValidationFailure>>> ResetPasswordAsync(ResetPasswordRequest model, CancellationToken ct = default);
        Task<Response<string, List<ValidationFailure>>> RecoveryPasswordAsync(RecoveryPasswordRequest model, CancellationToken ct = default);
        Task<Response<string, List<ValidationFailure>>> ValidateTokenAsync(string token, CancellationToken ct = default);

        /// <summary>
        /// Rotates the refresh-token cookie. Returns a fresh access token in the
        /// body and a fresh refresh cookie on the response. Reuse of an
        /// already-replaced token revokes the entire chain (B18).
        /// </summary>
        Task<Response<AuthWithRefreshResponse, List<ValidationFailure>>> RefreshAsync(string? refreshToken, string? ip, CancellationToken ct = default);

        /// <summary>Revokes the supplied refresh token. Idempotent.</summary>
        Task<Response<string, List<ValidationFailure>>> LogoutAsync(string? refreshToken, string? ip, CancellationToken ct = default);
    }
}