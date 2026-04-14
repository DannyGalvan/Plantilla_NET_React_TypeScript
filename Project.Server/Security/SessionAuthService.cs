using Project.Server.Entities.Dtos.Security;
using Project.Server.Services.Interfaces;
using System.Text.Json;
using Lombok.NET;

namespace Project.Server.Security
{
    /// <summary>
    /// Defines the <see cref="SessionAuthService" />
    /// </summary>
    [AllArgsConstructor]
    public partial class SessionAuthService
    {
        /// <summary>
        /// Defines the _securityAuthService
        /// </summary>
        private readonly ISecurityAuthService _securityAuthService;

        /// <summary>
        /// Validates user credentials
        /// </summary>
        /// <param name="userName">The username</param>
        /// <param name="password">The password</param>
        /// <param name="httpContext">The HTTP context</param>
        /// <returns>The <see cref="Task{AuthValidationResult}"/></returns>
        public async Task<AuthValidationResult> ValidateCredentialsAsync(string userName, string password, HttpContext httpContext)
        {
            try
            {
                var result = await _securityAuthService.ValidateCredentialsAsync(new SecurityLoginRequest
                {
                    UserName = userName,
                    Password = password,
                    IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = httpContext.Request.Headers.UserAgent.ToString()
                });

                return AuthValidationResult.Ok(result, result.RequiresPasswordChange);
            }
            catch (UnauthorizedAccessException ex)
            {
                return AuthValidationResult.Fail(ex.Message);
            }
            catch (Exception ex)
            {
                return AuthValidationResult.Fail("Error al validar credenciales");
            }
        }

        /// <summary>
        /// Validates password policy
        /// </summary>
        /// <param name="password">The password</param>
        /// <param name="message">The validation message</param>
        /// <returns>True if valid, false otherwise</returns>
        public bool ValidatePasswordPolicy(string password, out string message)
        {
            return _securityAuthService.ValidatePasswordPolicy(password, out message);
        }

        /// <summary>
        /// Changes user password
        /// </summary>
        /// <param name="userId">The user identifier</param>
        /// <param name="currentPassword">The current password</param>
        /// <param name="newPassword">The new password</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            return await _securityAuthService.ChangePasswordAsync(userId, new SecurityChangePasswordRequest
            {
                CurrentPassword = currentPassword,
                NewPassword = newPassword,
                ConfirmPassword = newPassword
            });
        }

        /// <summary>
        /// Sets password for a user
        /// </summary>
        /// <param name="userId">The user identifier</param>
        /// <param name="newPassword">The new password</param>
        /// <param name="forceChange">Indicates if user must change password on next login</param>
        /// <returns>The <see cref="Task"/></returns>
        public async Task SetPasswordAsync(int userId, string newPassword, bool forceChange)
        {
            await _securityAuthService.SetPasswordAsync(userId, newPassword, forceChange);
        }

        /// <summary>
        /// Signs in a user by setting session data
        /// </summary>
        /// <param name="httpContext">The HTTP context</param>
        /// <param name="user">The authenticated user</param>
        /// <returns>The <see cref="Task"/></returns>
        public async Task SignInAsync(HttpContext httpContext, SecurityAuthResult user)
        {
            var operations = await _securityAuthService.GetAllowedOperationsAsync(user.RoleId);

            httpContext.Session.SetInt32(SessionKeys.UserId, user.AppUserId);
            httpContext.Session.SetString(SessionKeys.UserName, user.UserName);
            httpContext.Session.SetString(SessionKeys.FullName, user.FullName);
            httpContext.Session.SetInt32(SessionKeys.RoleId, user.RoleId);
            httpContext.Session.SetString(SessionKeys.RoleName, user.RoleName);
            httpContext.Session.SetString(SessionKeys.AllowedOperations, JsonSerializer.Serialize(operations));
            httpContext.Session.SetString(SessionKeys.MustChangePassword, user.RequiresPasswordChange.ToString());
        }

        /// <summary>
        /// Signs out a user by clearing session
        /// </summary>
        /// <param name="httpContext">The HTTP context</param>
        public void SignOut(HttpContext httpContext)
        {
            httpContext.Session.Clear();
        }
    }
}
