namespace Project.Server.Configs.Models
{
    /// <summary>
    /// Defines the <see cref="AppSettings" />
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// Gets or sets the Secret
        /// </summary>
        public string Secret { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Email
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Password
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Host
        /// </summary>
        public string Host { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Port
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Gets or sets the TokenExpirationHrs
        /// </summary>
        public double TokenExpirationHrs { get; set; }

        /// <summary>
        /// Gets or sets the NotBefore
        /// </summary>
        public double NotBefore { get; set; }
    }
}
