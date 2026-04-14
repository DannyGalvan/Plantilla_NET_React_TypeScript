namespace Project.Server.Entities.Dtos.Security
{
    /// <summary>
    /// Defines the <see cref="SecurityPasswordPolicy" />
    /// </summary>
    public class SecurityPasswordPolicy
    {
        /// <summary>
        /// Gets or sets the MinimumLength
        /// </summary>
        public int MinimumLength { get; set; } = 8;

        /// <summary>
        /// Gets or sets a value indicating whether RequireUppercase
        /// </summary>
        public bool RequireUppercase { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether RequireLowercase
        /// </summary>
        public bool RequireLowercase { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether RequireDigit
        /// </summary>
        public bool RequireDigit { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether RequireSpecialCharacter
        /// </summary>
        public bool RequireSpecialCharacter { get; set; } = true;

        /// <summary>
        /// Gets or sets the PasswordHistoryLimit
        /// </summary>
        public int PasswordHistoryLimit { get; set; } = 5;

        /// <summary>
        /// Gets or sets the MaxPasswordAge (in days)
        /// </summary>
        public int MaxPasswordAge { get; set; } = 90;
    }
}
