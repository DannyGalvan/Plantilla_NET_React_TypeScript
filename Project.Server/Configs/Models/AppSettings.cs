namespace Project.Server.Configs.Models
{
    /// <summary>
    /// Strongly-typed <c>AppSettings</c> section. Bind from user-secrets or
    /// environment variables (e.g. <c>AppSettings__Secret</c>) — never commit a
    /// populated value to the repository.
    /// </summary>
    public class AppSettings
    {
        /// <summary>HMAC-SHA256 signing key. Must be ≥ 32 bytes (UTF-8).</summary>
        public string Secret { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }

        /// <summary>JWT <c>iss</c> claim. Defaults to <c>Plantilla</c>.</summary>
        public string Issuer { get; set; } = string.Empty;

        /// <summary>JWT <c>aud</c> claim. Defaults to <c>Plantilla.Client</c>.</summary>
        public string Audience { get; set; } = string.Empty;

        /// <summary>Access-token lifetime in hours. Defaults to 1.</summary>
        public double TokenExpirationHrs { get; set; }

        public double NotBefore { get; set; }

        /// <summary>
        /// Admin seed password used by the IHostedService that bootstraps the
        /// SA user when none exists. Must be supplied via user-secret or
        /// environment variable — never committed.
        /// </summary>
        public string? SeedAdminPassword { get; set; }

        /// <summary>
        /// Role id assigned to self-registered users (defaults to 2 to keep
        /// backward compatibility with the historical seed).
        /// </summary>
        public long? RegisterRoleId { get; set; }

        /// <summary>CORS allowlist. Empty array means no cross-origin traffic.</summary>
        public string[] CorsAllowedOrigins { get; set; } = Array.Empty<string>();
    }
}