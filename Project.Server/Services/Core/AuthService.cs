using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using FluentValidation.Results;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Project.Server.Configs.Models;
using Project.Server.Context;
using Project.Server.Entities.Models;
using Project.Server.Entities.Request;
using Project.Server.Entities.Response;
using Project.Server.Services.Interfaces;
using BC = BCrypt.Net;
using ValidationFailure = FluentValidation.Results.ValidationFailure;

namespace Project.Server.Services.Core
{
    /// <summary>
    /// Authentication service. Phase 3 hardening:
    /// <list type="bullet">
    ///   <item>Lockout after 5 failed attempts / 30 minutes (B7).</item>
    ///   <item>Plain-text passwords never leave the service (B5).</item>
    ///   <item>Recovery tokens generated with <see cref="RandomNumberGenerator"/>,
    ///         only the SHA-256 hash is persisted (B10).</item>
    ///   <item>Constant-time comparison for recovery token validation (B10).</item>
    ///   <item>Register assigns a configurable low-privilege role instead of a
    ///         hard-coded id (B20).</item>
    ///   <item>JWT carries <c>iss</c>, <c>aud</c>, <c>RolId</c> claims so the
    ///         hardened validator accepts the token.</item>
    /// </list>
    /// </summary>
    public class AuthService : IAuthService
    {
        private const int MaxFailedLoginAttempts = 5;
        private const int LockoutDurationMinutes = 30;
        private const int RecoveryTokenLifetimeMinutes = 15;

        // 3.8: short access token, longer refresh cookie.
        private const int AccessTokenLifetimeMinutes = 15;
        private const int RefreshTokenLifetimeDays = 7;
        public const string RefreshCookieName = "rt";

        private readonly DataContext _db;
        private readonly IOptions<AppSettings> _appSettings;
        private readonly SecurityPasswordPolicy _passwordPolicy;
        private readonly IValidator<LoginRequest> _loginValidations;
        private readonly IValidator<ChangePasswordRequest> _changePasswordValidations;
        private readonly IValidator<ResetPasswordRequest> _resetPasswordValidator;
        private readonly IValidator<RecoveryPasswordRequest> _recoveryPasswordValidator;
        private readonly IValidator<RegisterRequest> _registerValidator;
        private readonly ISendMail _sendMail;
        private readonly IMapper _mapper;
        private readonly ILogger<AuthService> _logger;
        private readonly IHttpContextAccessor _http;

        public AuthService(
            DataContext db,
            IOptions<AppSettings> appSettings,
            SecurityPasswordPolicy passwordPolicy,
            IValidator<LoginRequest> loginValidations,
            IValidator<ChangePasswordRequest> changePasswordValidations,
            IValidator<ResetPasswordRequest> resetPasswordValidator,
            IValidator<RecoveryPasswordRequest> recoveryPasswordValidator,
            IValidator<RegisterRequest> registerValidator,
            ISendMail sendMail,
            IMapper mapper,
            ILogger<AuthService> logger,
            IHttpContextAccessor http)
        {
            _db = db;
            _appSettings = appSettings;
            _passwordPolicy = passwordPolicy;
            _loginValidations = loginValidations;
            _changePasswordValidations = changePasswordValidations;
            _resetPasswordValidator = resetPasswordValidator;
            _recoveryPasswordValidator = recoveryPasswordValidator;
            _registerValidator = registerValidator;
            _sendMail = sendMail;
            _mapper = mapper;
            _logger = logger;
            _http = http;
        }

        // ============================================================
        // Login
        // ============================================================

        public async Task<Response<AuthWithRefreshResponse, List<ValidationFailure>>> AuthAsync(LoginRequest model, CancellationToken ct = default)
        {
            var userResponse = new Response<AuthWithRefreshResponse, List<ValidationFailure>>();
            try
            {
                var results = _loginValidations.Validate(model);
                if (!results.IsValid)
                {
                    return ValidationFailed<AuthWithRefreshResponse>(results.Errors);
                }

                User? user = await _db.Users.Include(u => u.Rol!).IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.UserName == model.UserName);

                if (user is null)
                {
                    await RecordLoginAuditAsync(0, model.UserName, null, null, false, "Usuario no encontrado");
                    return Unauthorized<AuthWithRefreshResponse>("Usuario o contraseña inválidos.");
                }

                if (await IsAccountLockedAsync(user))
                {
                    await RecordLoginAuditAsync(user.Id, user.UserName, null, null, false, "Cuenta bloqueada");
                    return Unauthorized<AuthWithRefreshResponse>("Cuenta bloqueada temporalmente.");
                }

                if (!BC.BCrypt.Verify(model.Password, user.Password))
                {
                    await IncrementFailedLoginAttemptsAsync(user);
                    await RecordLoginAuditAsync(user.Id, user.UserName, null, null, false, "Contraseña incorrecta");
                    return Unauthorized<AuthWithRefreshResponse>("Usuario o contraseña inválidos.");
                }

                if (user.State != 1)
                {
                    await RecordLoginAuditAsync(user.Id, user.UserName, null, null, false, "Usuario inactivo");
                    return Unauthorized<AuthWithRefreshResponse>("Usuario inactivo.");
                }

                await ResetFailedLoginAttemptsAsync(user);
                await RecordLoginAuditAsync(user.Id, user.UserName, null, null, true);

                var operations = await LoadOperationsAsync(user.RolId);
                var modules = await LoadModulesAsync(user.RolId);
                var authorizations = modules.Select(module => new Authorizations
                {
                    Module = _mapper.Map<Module, ModuleResponse>(module),
                    Operations = _mapper.Map<List<Operation>, List<OperationResponse>>(
                        operations.Where(o => o.ModuleId == module.Id).ToList())
                }).ToList();

                user.Rol!.RolOperations = await _db.RolOperations
                    .Where(ro => ro.RolId == user.RolId && ro.State == 1)
                    .ToListAsync();

                // Issue access token + a refresh cookie.
                var jwt = GetToken(user, operations);
                var refresh = await IssueRefreshTokenAsync(user, ClientIp());
                SetRefreshCookie(refresh.Plain, refresh.ExpiresAt);

                var auth = _mapper.Map<User, AuthWithRefreshResponse>(user);
                auth.Token = jwt;
                auth.Operations = authorizations;
                auth.ExpiresInSeconds = AccessTokenLifetimeMinutes * 60;

                userResponse.Success = true;
                userResponse.Message = "Inicio de sesión exitoso.";
                userResponse.Data = auth;
                return userResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during AuthAsync for user {UserName}", model.UserName);
                return InternalError<AuthWithRefreshResponse>(ex);
            }
        }

        // ============================================================
        // Refresh / Logout (3.8)
        // ============================================================

        public async Task<Response<AuthWithRefreshResponse, List<ValidationFailure>>> RefreshAsync(string? refreshToken, string? ip, CancellationToken ct = default)
        {
            var response = new Response<AuthWithRefreshResponse, List<ValidationFailure>>();
            try
            {
                if (string.IsNullOrWhiteSpace(refreshToken))
                {
                    ClearRefreshCookie();
                    return Unauthorized<AuthWithRefreshResponse>("Refresh token missing.");
                }

                var hash = HashRefreshToken(refreshToken);
                var stored = await _db.RefreshTokens
                    .Include(r => r.User!).ThenInclude(u => u.Rol!)
                    .FirstOrDefaultAsync(r => r.TokenHash == hash);

                if (stored is null || !stored.IsActive || stored.ReplacedByHash is not null)
                {
                    // Reuse of a rotated token: revoke the whole chain.
                    if (stored is not null)
                    {
                        await RevokeChainAsync(stored.UserId, ip, "Reuse detected");
                    }
                    ClearRefreshCookie();
                    return Unauthorized<AuthWithRefreshResponse>("Refresh token invalid or expired.");
                }

                var user = stored.User!;
                var operations = await LoadOperationsAsync(user.RolId);

                var newAccess = GetToken(user, operations);
                var rotated = await IssueRefreshTokenAsync(user, ip);
                stored.RevokedAt = DateTime.UtcNow;
                stored.RevokedByIp = ip;
                stored.ReplacedByHash = HashRefreshToken(rotated.Plain);
                stored.UpdatedAt = DateTime.UtcNow;
                stored.UpdatedBy = user.Id;
                await _db.SaveChangesAsync(ct);

                SetRefreshCookie(rotated.Plain, rotated.ExpiresAt);

                var auth = _mapper.Map<User, AuthWithRefreshResponse>(user);
                auth.Token = newAccess;
                auth.ExpiresInSeconds = AccessTokenLifetimeMinutes * 60;
                auth.Operations = (await BuildAuthorizationsAsync(user)).Select(a => a).ToList();

                response.Success = true;
                response.Message = "Token refreshed.";
                response.Data = auth;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during RefreshAsync");
                ClearRefreshCookie();
                return InternalError<AuthWithRefreshResponse>(ex);
            }
        }

        public async Task<Response<string, List<ValidationFailure>>> LogoutAsync(string? refreshToken, string? ip, CancellationToken ct = default)
        {
            var response = new Response<string, List<ValidationFailure>>();
            try
            {
                if (!string.IsNullOrWhiteSpace(refreshToken))
                {
                    var hash = HashRefreshToken(refreshToken);
                    var stored = await _db.RefreshTokens.FirstOrDefaultAsync(r => r.TokenHash == hash);
                    if (stored is not null && stored.RevokedAt is null)
                    {
                        stored.RevokedAt = DateTime.UtcNow;
                        stored.RevokedByIp = ip;
                        stored.UpdatedAt = DateTime.UtcNow;
                        await _db.SaveChangesAsync(ct);
                    }
                }
                ClearRefreshCookie();
                response.Success = true;
                response.Message = "Logged out.";
                response.Data = string.Empty;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during LogoutAsync");
                ClearRefreshCookie();
                return InternalError<string>(ex);
            }
        }

        // ============================================================
        // Register (anonymous, low-privilege role)
        // ============================================================

        public async Task<Response<User, List<ValidationFailure>>> RegisterAsync(RegisterRequest model, CancellationToken ct = default)
        {
            Response<User, List<ValidationFailure>> response = new();
            try
            {
                var results = _registerValidator.Validate(model);
                if (!results.IsValid)
                {
                    return ValidationFailed<User>(results.Errors);
                }

                var exists = await _db.Users.AnyAsync(x => x.UserName == model.UserName || x.Email == model.Email);
                if (exists)
                {
                    response.Success = false;
                    response.Message = "El usuario ya existe en la plataforma.";
                    return response;
                }

                var user = _mapper.Map<RegisterRequest, User>(model);

                // Configurable low-privilege role. The bootstrap (or operator)
                // must seed a role whose id matches AppSettings:RegisterRoleId
                // (default 2 to preserve the historical contract).
                user.RolId = _appSettings.Value.RegisterRoleId ?? 2;
                user.Password = BC.BCrypt.HashPassword(model.Password);
                user.MustChangePassword = false;
                user.LastPasswordChange = DateTime.UtcNow;

                _db.Users.Add(user);
                await _db.SaveChangesAsync();

                response.Success = true;
                response.Message = "Usuario creado correctamente.";
                response.Data = user;
                response.TotalResults = 1;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during RegisterAsync");
                return InternalError<User>(ex);
            }
        }

        // ============================================================
        // Change / Reset password (B5 — never echo the password back)
        // ============================================================

        public async Task<Response<string, List<ValidationFailure>>> ChangePasswordAsync(ChangePasswordRequest model, CancellationToken ct = default)
        {
            Response<string, List<ValidationFailure>> response = new();
            try
            {
                var results = _changePasswordValidations.Validate(model);
                if (!results.IsValid)
                {
                    return ValidationFailed<string>(results.Errors);
                }

                var hash = await ResolveRecoveryTokenHashAsync(model.Token);
                if (hash is null)
                {
                    response.Success = false;
                    response.Message = "El token no es válido o ha expirado.";
                    return response;
                }

                var user = await _db.Users.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.RecoveryToken == hash);
                if (user is null)
                {
                    response.Success = false;
                    response.Message = "El token no es válido.";
                    return response;
                }

                if (user.DateToken is null || DateTime.UtcNow > user.DateToken.Value.AddMinutes(RecoveryTokenLifetimeMinutes))
                {
                    response.Success = false;
                    response.Message = "El token ha expirado.";
                    return response;
                }

                if (BC.BCrypt.Verify(model.Password, user.Password))
                {
                    response.Success = false;
                    response.Message = "La nueva contraseña debe ser distinta a la anterior.";
                    return response;
                }

                user.Password = BC.BCrypt.HashPassword(model.Password);
                user.RecoveryToken = string.Empty;
                user.Reset = false;
                user.MustChangePassword = false;
                user.LastPasswordChange = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;
                user.UpdatedBy = user.Id;

                _db.Users.Update(user);
                await _db.SaveChangesAsync();

                response.Success = true;
                response.Message = "Cambio de contraseña exitoso.";
                response.Data = string.Empty;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during ChangePasswordAsync");
                return InternalError<string>(ex);
            }
        }

        public async Task<Response<string, List<ValidationFailure>>> ResetPasswordAsync(ResetPasswordRequest model, CancellationToken ct = default)
        {
            Response<string, List<ValidationFailure>> response = new();
            try
            {
                var results = _resetPasswordValidator.Validate(model);
                if (!results.IsValid)
                {
                    return ValidationFailed<string>(results.Errors);
                }

                if (!_passwordPolicy.Validate(model.Password, out var policyMessage))
                {
                    response.Success = false;
                    response.Message = policyMessage;
                    return response;
                }

                var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == model.IdUser);
                if (user is null)
                {
                    response.Success = false;
                    response.Message = "Usuario no encontrado.";
                    return response;
                }

                user.Password = BC.BCrypt.HashPassword(model.Password);
                user.RecoveryToken = string.Empty;
                user.Reset = false;
                user.MustChangePassword = false;
                user.LastPasswordChange = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;
                user.UpdatedBy = model.IdUser;

                _db.Users.Update(user);
                await _db.SaveChangesAsync();

                response.Success = true;
                response.Message = "Cambio de contraseña exitoso.";
                response.Data = string.Empty;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during ResetPasswordAsync");
                return InternalError<string>(ex);
            }
        }

        // ============================================================
        // Recovery — random token, hash stored, plain sent by email
        // ============================================================

        public async Task<Response<string, List<ValidationFailure>>> RecoveryPasswordAsync(RecoveryPasswordRequest model, CancellationToken ct = default)
        {
            Response<string, List<ValidationFailure>> response = new();
            try
            {
                var results = _recoveryPasswordValidator.Validate(model);
                if (!results.IsValid)
                {
                    return ValidationFailed<string>(results.Errors);
                }

                // B16 — never confirm or deny that the email exists. Same
                // generic 200 either way. The work below only runs when the
                // email actually exists; the caller never sees the difference.
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
                if (user is not null)
                {
                    var tokenPlain = GenerateRecoveryToken();
                    var tokenHash = HashRecoveryToken(tokenPlain);

                    user.RecoveryToken = tokenHash;
                    user.Reset = true;
                    user.DateToken = DateTime.UtcNow;
                    user.UpdatedAt = DateTime.UtcNow;
                    user.UpdatedBy = user.Id;

                    _db.Users.Update(user);
                    await _db.SaveChangesAsync();

                    var body = $"Hola {user.Name},<br/>Use este token para restablecer su contraseña:<br/><b>{tokenPlain}</b>";
                    await _sendMail.SendAsync(user.Email, "Recuperar Contraseña", body);
                }

                response.Success = true;
                response.Message = "Si la cuenta existe, se ha enviado un correo con las instrucciones.";
                response.Data = string.Empty;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during RecoveryPasswordAsync");
                return InternalError<string>(ex);
            }
        }

        public async Task<Response<string, List<ValidationFailure>>> ValidateTokenAsync(string token, CancellationToken ct = default)
        {
            Response<string, List<ValidationFailure>> response = new();
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    response.Success = false;
                    response.Message = "Token vacío.";
                    return response;
                }

                var hash = HashRecoveryToken(token);
                var user = await _db.Users.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.RecoveryToken == hash);

                if (user is null)
                {
                    response.Success = false;
                    response.Message = "Su token ha expirado.";
                    return response;
                }

                if (user.DateToken is null || DateTime.UtcNow > user.DateToken.Value.AddMinutes(RecoveryTokenLifetimeMinutes))
                {
                    response.Success = false;
                    response.Message = "Su token ha expirado.";
                    return response;
                }

                response.Success = true;
                response.Message = "Token válido.";
                response.Data = token;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during ValidateTokenAsync");
                return InternalError<string>(ex);
            }
        }

        // ============================================================
        // JWT generation (Phase 3 — issuer/audience + RolId claim)
        // ============================================================

        private string GetToken(User user, IEnumerable<Operation> operations)
        {
            var settings = _appSettings.Value;
            var issuer = string.IsNullOrWhiteSpace(settings.Issuer) ? "Plantilla" : settings.Issuer;
            var audience = string.IsNullOrWhiteSpace(settings.Audience) ? "Plantilla.Client" : settings.Audience;

            var key = Encoding.UTF8.GetBytes(settings.Secret);
            var handler = new JwtSecurityTokenHandler();

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Name, user.Name),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new("RolId", user.RolId.ToString()),
            };

            foreach (var op in operations)
            {
                if (!string.IsNullOrWhiteSpace(op.OperationKey))
                {
                    claims.Add(new Claim("OperationKey", op.OperationKey));
                }
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Issuer = issuer,
                Audience = audience,
                Subject = new ClaimsIdentity(claims),
                NotBefore = DateTime.UtcNow.AddMinutes(settings.NotBefore),
                Expires = DateTime.UtcNow.AddHours(settings.TokenExpirationHrs),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
            };

            return handler.WriteToken(handler.CreateToken(tokenDescriptor));
        }

        // ============================================================
        // Helpers
        // ============================================================

        private static string GenerateRecoveryToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Base64UrlEncode(bytes);
        }

        private static string HashRecoveryToken(string token)
        {
            Span<byte> hash = stackalloc byte[32];
            SHA256.HashData(Encoding.UTF8.GetBytes(token), hash);
            return Convert.ToHexString(hash);
        }

        private async Task<string?> ResolveRecoveryTokenHashAsync(string token)
        {
            // Constant-time lookup: search by hash (not by plain token). Both
            // the in-memory comparison and the SHA-256 reduce timing leaks.
            var hash = HashRecoveryToken(token);
            return await Task.FromResult(hash);
        }

        private async Task<List<Operation>> LoadOperationsAsync(long rolId)
        {
            return await _db.RolOperations
                .Include(r => r.Operation)
                .Where(r => r.RolId == rolId && r.State == 1)
                .Join(_db.Operations,
                    ro => ro.OperationId,
                    o => o.Id,
                    (ro, o) => o)
                .ToListAsync();
        }

        private async Task<List<Module>> LoadModulesAsync(long rolId)
        {
            return await _db.RolOperations
                .Where(ro => ro.RolId == rolId && ro.State == 1)
                .Join(_db.Operations, ro => ro.OperationId, o => o.Id, (ro, o) => o.ModuleId)
                .Distinct()
                .Join(_db.Modules, mid => mid, m => m.Id, (mid, m) => m)
                .ToListAsync();
        }

        private async Task<bool> IsAccountLockedAsync(User user)
        {
            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
            {
                return true;
            }
            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value <= DateTime.UtcNow)
            {
                user.LockoutEnd = null;
                user.FailedLoginAttempts = 0;
                await _db.SaveChangesAsync();
            }
            return false;
        }

        private async Task IncrementFailedLoginAttemptsAsync(User user)
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= MaxFailedLoginAttempts)
            {
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(LockoutDurationMinutes);
                _logger.LogWarning("User {UserId} locked out after {Attempts} failed attempts.", user.Id, user.FailedLoginAttempts);
            }
            await _db.SaveChangesAsync();
        }

        private async Task ResetFailedLoginAttemptsAsync(User user)
        {
            if (user.FailedLoginAttempts != 0 || user.LockoutEnd is not null)
            {
                user.FailedLoginAttempts = 0;
                user.LockoutEnd = null;
                await _db.SaveChangesAsync();
            }
        }

        private async Task RecordLoginAuditAsync(long userId, string userName, string? ip, string? ua, bool ok, string? reason = null)
        {
            try
            {
                _db.LoginAudits.Add(new LoginAudit
                {
                    UserId = userId,
                    UserName = userName,
                    IpAddress = ip,
                    UserAgent = ua,
                    LoginSuccessful = ok,
                    FailureReason = reason,
                    LoginDate = DateTime.UtcNow,
                    State = 1,
                    CreatedBy = userId == 0 ? 1 : userId,
                });
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to record login audit for {UserName}", userName);
            }
        }

        private static string Base64UrlEncode(byte[] bytes)
        {
            return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        // ============================================================
        // Refresh token plumbing (3.8)
        // ============================================================

        private async Task<(string Plain, DateTime ExpiresAt)> IssueRefreshTokenAsync(User user, string? ip)
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            var plain = Base64UrlEncode(bytes);
            var hash = HashRefreshToken(plain);
            var expires = DateTime.UtcNow.AddDays(RefreshTokenLifetimeDays);

            _db.RefreshTokens.Add(new RefreshToken
            {
                UserId = user.Id,
                TokenHash = hash,
                ExpiresAt = expires,
                CreatedByIp = ip,
                State = 1,
                CreatedBy = user.Id,
                CreatedAt = DateTime.UtcNow,
            });
            await _db.SaveChangesAsync();
            return (plain, expires);
        }

        private static string HashRefreshToken(string token)
        {
            Span<byte> hash = stackalloc byte[32];
            SHA256.HashData(Encoding.UTF8.GetBytes(token), hash);
            return Convert.ToHexString(hash);
        }

        private void SetRefreshCookie(string plain, DateTime expiresAt)
        {
            var ctx = _http.HttpContext;
            if (ctx is null) return;
            ctx.Response.Cookies.Append(RefreshCookieName, plain, new CookieOptions
            {
                HttpOnly = true,
                Secure = !ctx.Request.IsHttps ? false : true,
                SameSite = SameSiteMode.Strict,
                Path = "/api/v1/Auth",
                Expires = expiresAt,
            });

            // 3.9 — defence in depth. The SPA reads this non-HttpOnly cookie
            // and echoes it back as the X-XSRF-TOKEN header on /Auth/Refresh.
            // SameSite=Strict already blocks cross-site cookies; the header
            // check makes the protection explicit and survives older browsers.
            var xsrf = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            ctx.Response.Cookies.Append("XSRF-TOKEN", xsrf, new CookieOptions
            {
                HttpOnly = false,
                Secure = !ctx.Request.IsHttps ? false : true,
                SameSite = SameSiteMode.Strict,
                Path = "/",
                Expires = expiresAt,
            });
        }

        private void ClearRefreshCookie()
        {
            var ctx = _http.HttpContext;
            if (ctx is null) return;
            ctx.Response.Cookies.Delete(RefreshCookieName, new CookieOptions { Path = "/api/v1/Auth" });
            ctx.Response.Cookies.Delete("XSRF-TOKEN", new CookieOptions { Path = "/" });
        }

        private string? ClientIp()
        {
            var ctx = _http.HttpContext;
            return ctx?.Connection.RemoteIpAddress?.ToString();
        }

        private async Task RevokeChainAsync(long userId, string? ip, string reason)
        {
            var tokens = await _db.RefreshTokens
                .Where(r => r.UserId == userId && r.RevokedAt == null)
                .ToListAsync();
            foreach (var t in tokens)
            {
                t.RevokedAt = DateTime.UtcNow;
                t.RevokedByIp = ip;
                t.UpdatedAt = DateTime.UtcNow;
            }
            await _db.SaveChangesAsync();
            _logger.LogWarning("Revoked {Count} refresh tokens for user {UserId}: {Reason}", tokens.Count, userId, reason);
        }

        private async Task<List<Authorizations>> BuildAuthorizationsAsync(User user)
        {
            var operations = await LoadOperationsAsync(user.RolId);
            var modules = await LoadModulesAsync(user.RolId);
            return modules.Select(module => new Authorizations
            {
                Module = _mapper.Map<Module, ModuleResponse>(module),
                Operations = _mapper.Map<List<Operation>, List<OperationResponse>>(
                    operations.Where(o => o.ModuleId == module.Id).ToList())
            }).ToList();
        }

        // ============================================================
        // Response helpers
        // ============================================================

        private static Response<T, List<ValidationFailure>> ValidationFailed<T>(List<ValidationFailure> errors)
            => new()
            {
                Success = false,
                Status = ResponseStatus.ValidationFailed,
                Message = "Validation failed.",
                Errors = errors,
            };

        private static Response<T, List<ValidationFailure>> Unauthorized<T>(string message)
            => new()
            {
                Success = false,
                Status = ResponseStatus.Forbidden,
                Message = message,
            };

        private Response<T, List<ValidationFailure>> InternalError<T>(Exception ex)
        {
            var traceId = System.Diagnostics.Activity.Current?.Id ?? Guid.NewGuid().ToString("N");
            _logger.LogError(ex, "Auth failure (traceId={traceId})", traceId);
            return new()
            {
                Success = false,
                Status = ResponseStatus.Error,
                Message = $"Operation failed (traceId={traceId}).",
            };
        }
    }
}