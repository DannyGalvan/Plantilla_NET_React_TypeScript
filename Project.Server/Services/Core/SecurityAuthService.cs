using Lombok.NET;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Project.Server.Configs.Models;
using Project.Server.Context;
using Project.Server.Entities.Dtos.Security;
using Project.Server.Entities.Models;
using Project.Server.Services.Interfaces;
using System.Text.RegularExpressions;
using BC = BCrypt.Net;

namespace Project.Server.Services.Core
{
    /// <summary>
    /// Defines the <see cref="SecurityAuthService" />
    /// </summary>
    public partial class SecurityAuthService : ISecurityAuthService
    {
        /// <summary>
        /// Defines the _bd
        /// </summary>
        private readonly DataContext _bd;

        /// <summary>
        /// Defines the _logger
        /// </summary>
        private readonly ILogger<SecurityAuthService> _logger;

        /// <summary>
        /// Defines the _appSettings
        /// </summary>
        private readonly IOptions<AppSettings> _appSettings;

        /// <summary>
        /// Defines the password policy
        /// </summary>
        private readonly SecurityPasswordPolicy _passwordPolicy = new();

        /// <summary>
        /// Maximum failed login attempts before account lockout
        /// </summary>
        private const int MaxFailedLoginAttempts = 5;

        /// <summary>
        /// Lockout duration in minutes
        /// </summary>
        private const int LockoutDurationMinutes = 30;

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityAuthService"/> class.
        /// </summary>
        public SecurityAuthService(DataContext bd, ILogger<SecurityAuthService> logger, IOptions<AppSettings> appSettings)
        {
            _bd = bd;
            _logger = logger;
            _appSettings = appSettings;
        }

        /// <summary>
        /// Validates user credentials and returns authentication result
        /// </summary>
        public async Task<SecurityAuthResult> ValidateCredentialsAsync(SecurityLoginRequest request)
        {
            try
            {
                var user = await _bd.Users
                    .Include(u => u.Rol)
                    .FirstOrDefaultAsync(u => u.UserName == request.UserName);

                if (user == null)
                {
                    await RecordLoginAuditAsync(0, request.UserName, request.IpAddress, request.UserAgent, false, "Usuario no encontrado");
                    throw new UnauthorizedAccessException("Usuario o contraseña inválidos");
                }

                // Check if account is locked
                if (await IsAccountLockedAsync(user.Id))
                {
                    await RecordLoginAuditAsync(user.Id, user.UserName, request.IpAddress, request.UserAgent, false, "Cuenta bloqueada");
                    throw new UnauthorizedAccessException("Cuenta bloqueada temporalmente. Intente más tarde.");
                }

                // Verify password
                if (!BC.BCrypt.Verify(request.Password, user.Password))
                {
                    await IncrementFailedLoginAttemptsAsync(user.Id);
                    await RecordLoginAuditAsync(user.Id, user.UserName, request.IpAddress, request.UserAgent, false, "Contraseña incorrecta");
                    throw new UnauthorizedAccessException("Usuario o contraseña inválidos");
                }

                // Check if user is active
                if (user.State != 1)
                {
                    await RecordLoginAuditAsync(user.Id, user.UserName, request.IpAddress, request.UserAgent, false, "Usuario inactivo");
                    throw new UnauthorizedAccessException("Usuario inactivo");
                }

                // Reset failed login attempts on successful login
                await ResetFailedLoginAttemptsAsync(user.Id);

                // Record successful login
                await RecordLoginAuditAsync(user.Id, user.UserName, request.IpAddress, request.UserAgent, true);

                // Check if password change is required
                bool requiresPasswordChange = user.MustChangePassword;
                if (user.LastPasswordChange.HasValue && _passwordPolicy.MaxPasswordAge > 0)
                {
                    var passwordAge = (DateTime.Now - user.LastPasswordChange.Value).TotalDays;
                    if (passwordAge > _passwordPolicy.MaxPasswordAge)
                    {
                        requiresPasswordChange = true;
                    }
                }

                return new SecurityAuthResult
                {
                    AppUserId = (int)user.Id,
                    UserName = user.UserName,
                    FullName = user.Name,
                    Email = user.Email,
                    RoleId = (int)user.RolId,
                    RoleName = user.Rol?.Name ?? string.Empty,
                    RequiresPasswordChange = requiresPasswordChange
                };
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating credentials for user {UserName}", request.UserName);
                throw new Exception("Error al validar credenciales");
            }
        }

        /// <summary>
        /// Validates password against security policy
        /// </summary>
        public bool ValidatePasswordPolicy(string password, out string message)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                message = "La contraseña no puede estar vacía";
                return false;
            }

            if (password.Length < _passwordPolicy.MinimumLength)
            {
                message = $"La contraseña debe tener al menos {_passwordPolicy.MinimumLength} caracteres";
                return false;
            }

            if (_passwordPolicy.RequireUppercase && !Regex.IsMatch(password, @"[A-Z]"))
            {
                message = "La contraseña debe contener al menos una letra mayúscula";
                return false;
            }

            if (_passwordPolicy.RequireLowercase && !Regex.IsMatch(password, @"[a-z]"))
            {
                message = "La contraseña debe contener al menos una letra minúscula";
                return false;
            }

            if (_passwordPolicy.RequireDigit && !Regex.IsMatch(password, @"[0-9]"))
            {
                message = "La contraseña debe contener al menos un número";
                return false;
            }

            if (_passwordPolicy.RequireSpecialCharacter && !Regex.IsMatch(password, @"[!@#$%^&*(),.?""':{}|<>]"))
            {
                message = "La contraseña debe contener al menos un carácter especial";
                return false;
            }

            message = string.Empty;
            return true;
        }

        /// <summary>
        /// Changes user password
        /// </summary>
        public async Task<bool> ChangePasswordAsync(int userId, SecurityChangePasswordRequest request)
        {
            try
            {
                var user = await _bd.Users.FindAsync((long)userId);
                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found for password change", userId);
                    return false;
                }

                // Verify current password
                if (!BC.BCrypt.Verify(request.CurrentPassword, user.Password))
                {
                    _logger.LogWarning("Invalid current password for user {UserId}", userId);
                    return false;
                }

                // Validate new password matches confirmation
                if (request.NewPassword != request.ConfirmPassword)
                {
                    _logger.LogWarning("Password confirmation mismatch for user {UserId}", userId);
                    return false;
                }

                // Validate password policy
                if (!ValidatePasswordPolicy(request.NewPassword, out string policyMessage))
                {
                    _logger.LogWarning("Password policy violation for user {UserId}: {Message}", userId, policyMessage);
                    return false;
                }

                // Check if password was recently used
                if (await IsPasswordRecentlyUsedAsync(userId, request.NewPassword))
                {
                    _logger.LogWarning("Recently used password for user {UserId}", userId);
                    return false;
                }

                // Update password
                user.Password = BC.BCrypt.HashPassword(request.NewPassword);
                user.MustChangePassword = false;
                user.LastPasswordChange = DateTime.Now;
                user.UpdatedAt = DateTime.Now;

                // Save password to history
                _bd.PasswordHistories.Add(new PasswordHistory
                {
                    UserId = user.Id,
                    PasswordHash = user.Password,
                    ChangedBy = user.Id,
                    ForceChange = false
                });

                await _bd.SaveChangesAsync();
                _logger.LogInformation("Password changed successfully for user {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// Sets a new password for a user (admin operation)
        /// </summary>
        public async Task SetPasswordAsync(int userId, string newPassword, bool forceChange)
        {
            try
            {
                var user = await _bd.Users.FindAsync((long)userId);
                if (user == null)
                {
                    throw new ArgumentException($"User {userId} not found");
                }

                user.Password = BC.BCrypt.HashPassword(newPassword);
                user.MustChangePassword = forceChange;
                user.LastPasswordChange = DateTime.Now;
                user.UpdatedAt = DateTime.Now;

                // Save password to history
                _bd.PasswordHistories.Add(new PasswordHistory
                {
                    UserId = user.Id,
                    PasswordHash = user.Password,
                    ChangedBy = user.Id,
                    ForceChange = forceChange
                });

                await _bd.SaveChangesAsync();
                _logger.LogInformation("Password set for user {UserId} (ForceChange: {ForceChange})", userId, forceChange);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting password for user {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Gets all allowed operations for a role
        /// </summary>
        public async Task<HashSet<string>> GetAllowedOperationsAsync(int roleId)
        {
            try
            {
                var operations = await _bd.RolOperations
                    .Include(ro => ro.Operation)
                    .Where(ro => ro.RolId == roleId && ro.State == 1)
                    .Select(ro => ro.Operation!.Policy)
                    .ToListAsync();

                return new HashSet<string>(operations, StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting allowed operations for role {RoleId}", roleId);
                return new HashSet<string>();
            }
        }

        /// <summary>
        /// Validates if password was used recently
        /// </summary>
        public async Task<bool> IsPasswordRecentlyUsedAsync(int userId, string password)
        {
            try
            {
                var recentPasswords = await _bd.PasswordHistories
                    .Where(ph => ph.UserId == userId && ph.State == 1)
                    .OrderByDescending(ph => ph.ChangedAt)
                    .Take(_passwordPolicy.PasswordHistoryLimit)
                    .Select(ph => ph.PasswordHash)
                    .ToListAsync();

                foreach (var hash in recentPasswords)
                {
                    if (BC.BCrypt.Verify(password, hash))
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking password history for user {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// Records login audit entry
        /// </summary>
        public async Task RecordLoginAuditAsync(long userId, string userName, string? ipAddress, string? userAgent, bool success, string? failureReason = null)
        {
            try
            {
                var audit = new LoginAudit
                {
                    UserId = userId,
                    UserName = userName,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    LoginSuccessful = success,
                    FailureReason = failureReason,
                    LoginDate = DateTime.Now
                };

                _bd.LoginAudits.Add(audit);
                await _bd.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording login audit for user {UserName}", userName);
            }
        }

        /// <summary>
        /// Checks if user account is locked
        /// </summary>
        public async Task<bool> IsAccountLockedAsync(long userId)
        {
            try
            {
                var user = await _bd.Users.FindAsync(userId);
                if (user == null) return false;

                if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.Now)
                {
                    return true;
                }

                // Clear lockout if expired
                if (user.LockoutEnd.HasValue && user.LockoutEnd.Value <= DateTime.Now)
                {
                    user.LockoutEnd = null;
                    user.FailedLoginAttempts = 0;
                    await _bd.SaveChangesAsync();
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking account lock status for user {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// Increments failed login attempts and locks account if threshold is reached
        /// </summary>
        public async Task IncrementFailedLoginAttemptsAsync(long userId)
        {
            try
            {
                var user = await _bd.Users.FindAsync(userId);
                if (user == null) return;

                user.FailedLoginAttempts++;

                if (user.FailedLoginAttempts >= MaxFailedLoginAttempts)
                {
                    user.LockoutEnd = DateTime.Now.AddMinutes(LockoutDurationMinutes);
                    _logger.LogWarning("User {UserId} locked out due to {Attempts} failed login attempts", userId, user.FailedLoginAttempts);
                }

                await _bd.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error incrementing failed login attempts for user {UserId}", userId);
            }
        }

        /// <summary>
        /// Resets failed login attempts
        /// </summary>
        public async Task ResetFailedLoginAttemptsAsync(long userId)
        {
            try
            {
                var user = await _bd.Users.FindAsync(userId);
                if (user == null) return;

                user.FailedLoginAttempts = 0;
                user.LockoutEnd = null;
                await _bd.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting failed login attempts for user {UserId}", userId);
            }
        }
    }
}
