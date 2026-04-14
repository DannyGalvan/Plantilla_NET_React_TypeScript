using Project.Server.Entities.Dtos.Security;

namespace Project.Server.Services.Interfaces
{
    /// <summary>
    /// Defines the <see cref="ISecurityAuthService" />
    /// </summary>
    public interface ISecurityAuthService
    {
        /// <summary>
        /// Validates user credentials and returns authentication result
        /// </summary>
        /// <param name="request">The request<see cref="SecurityLoginRequest"/></param>
        /// <returns>The <see cref="Task{SecurityAuthResult}"/></returns>
        Task<SecurityAuthResult> ValidateCredentialsAsync(SecurityLoginRequest request);

        /// <summary>
        /// Validates password against security policy
        /// </summary>
        /// <param name="password">The password to validate</param>
        /// <param name="message">The validation message</param>
        /// <returns>True if password is valid, false otherwise</returns>
        bool ValidatePasswordPolicy(string password, out string message);

        /// <summary>
        /// Changes user password
        /// </summary>
        /// <param name="userId">The user identifier</param>
        /// <param name="request">The request<see cref="SecurityChangePasswordRequest"/></param>
        /// <returns>True if password was changed successfully, false otherwise</returns>
        Task<bool> ChangePasswordAsync(int userId, SecurityChangePasswordRequest request);

        /// <summary>
        /// Sets a new password for a user (admin operation)
        /// </summary>
        /// <param name="userId">The user identifier</param>
        /// <param name="newPassword">The new password</param>
        /// <param name="forceChange">Indicates if user must change password on next login</param>
        /// <returns>The <see cref="Task"/></returns>
        Task SetPasswordAsync(int userId, string newPassword, bool forceChange);

        /// <summary>
        /// Gets all allowed operations for a role
        /// </summary>
        /// <param name="roleId">The role identifier</param>
        /// <returns>The <see cref="Task{HashSet{string}}"/></returns>
        Task<HashSet<string>> GetAllowedOperationsAsync(int roleId);

        /// <summary>
        /// Validates if password was used recently
        /// </summary>
        /// <param name="userId">The user identifier</param>
        /// <param name="password">The password to check</param>
        /// <returns>True if password was used recently, false otherwise</returns>
        Task<bool> IsPasswordRecentlyUsedAsync(int userId, string password);

        /// <summary>
        /// Records login audit entry
        /// </summary>
        /// <param name="userId">The user identifier</param>
        /// <param name="userName">The username</param>
        /// <param name="ipAddress">The IP address</param>
        /// <param name="userAgent">The user agent</param>
        /// <param name="success">Indicates if login was successful</param>
        /// <param name="failureReason">The failure reason if login failed</param>
        /// <returns>The <see cref="Task"/></returns>
        Task RecordLoginAuditAsync(long userId, string userName, string? ipAddress, string? userAgent, bool success, string? failureReason = null);

        /// <summary>
        /// Checks if user account is locked
        /// </summary>
        /// <param name="userId">The user identifier</param>
        /// <returns>True if account is locked, false otherwise</returns>
        Task<bool> IsAccountLockedAsync(long userId);

        /// <summary>
        /// Increments failed login attempts and locks account if threshold is reached
        /// </summary>
        /// <param name="userId">The user identifier</param>
        /// <returns>The <see cref="Task"/></returns>
        Task IncrementFailedLoginAttemptsAsync(long userId);

        /// <summary>
        /// Resets failed login attempts
        /// </summary>
        /// <param name="userId">The user identifier</param>
        /// <returns>The <see cref="Task"/></returns>
        Task ResetFailedLoginAttemptsAsync(long userId);
    }
}
