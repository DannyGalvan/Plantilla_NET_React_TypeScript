namespace Project.Server.Entities.Dtos.Security
{
    /// <summary>
    /// Defines the <see cref="SecurityLoginRequest" />
    /// </summary>
    public class SecurityLoginRequest
    {
        /// <summary>
        /// Gets or sets the UserName
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Password
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the IpAddress
        /// </summary>
        public string? IpAddress { get; set; }

        /// <summary>
        /// Gets or sets the UserAgent
        /// </summary>
        public string? UserAgent { get; set; }
    }
}
