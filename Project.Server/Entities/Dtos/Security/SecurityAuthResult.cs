namespace Project.Server.Entities.Dtos.Security
{
    /// <summary>
    /// Defines the <see cref="SecurityAuthResult" />
    /// </summary>
    public class SecurityAuthResult
    {
        /// <summary>
        /// Gets or sets the AppUserId
        /// </summary>
        public int AppUserId { get; set; }

        /// <summary>
        /// Gets or sets the UserName
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the FullName
        /// </summary>
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Email
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the RoleId
        /// </summary>
        public int RoleId { get; set; }

        /// <summary>
        /// Gets or sets the RoleName
        /// </summary>
        public string RoleName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether RequiresPasswordChange
        /// </summary>
        public bool RequiresPasswordChange { get; set; }

        /// <summary>
        /// Gets or sets the Token
        /// </summary>
        public string? Token { get; set; }
    }
}
