namespace Project.Server.Entities.Response
{
    /// <summary>
    /// Defines the <see cref="AuthResponse" />
    /// </summary>
    public class AuthResponse
    {
        /// <summary>
        /// Gets or sets the Name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the UserName
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Email
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Token
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether Redirect
        /// </summary>
        public bool Redirect { get; set; }

        /// <summary>
        /// Gets or sets the UserId
        /// </summary>
        public long UserId { get; set; }

        /// <summary>
        /// Gets or sets the Role
        /// </summary>
        public long Rol { get; set; }

        /// <summary>
        /// Gets or sets the Operations
        /// </summary>
        public ICollection<Authorizations> Operations { get; set; } = new List<Authorizations>();

    }
}
