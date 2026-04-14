namespace Project.Server.Entities.Dtos.Security
{
    /// <summary>
    /// Defines the <see cref="SecurityChangePasswordRequest" />
    /// </summary>
    public class SecurityChangePasswordRequest
    {
        /// <summary>
        /// Gets or sets the CurrentPassword
        /// </summary>
        public string CurrentPassword { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the NewPassword
        /// </summary>
        public string NewPassword { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the ConfirmPassword
        /// </summary>
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
