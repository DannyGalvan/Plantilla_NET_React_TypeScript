namespace Project.Server.Entities.Dtos.Security
{
    /// <summary>
    /// Defines the <see cref="AuthValidationResult" />
    /// </summary>
    public class AuthValidationResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether Succeeded
        /// </summary>
        public bool Succeeded { get; set; }

        /// <summary>
        /// Gets or sets the ErrorMessage
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the User
        /// </summary>
        public SecurityAuthResult? User { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether RequiresPasswordChange
        /// </summary>
        public bool RequiresPasswordChange { get; set; }

        /// <summary>
        /// Creates a successful validation result
        /// </summary>
        /// <param name="user">The user<see cref="SecurityAuthResult"/></param>
        /// <param name="requiresPasswordChange">Indicates if password change is required</param>
        /// <returns>The <see cref="AuthValidationResult"/></returns>
        public static AuthValidationResult Ok(SecurityAuthResult user, bool requiresPasswordChange = false)
        {
            return new AuthValidationResult
            {
                Succeeded = true,
                User = user,
                RequiresPasswordChange = requiresPasswordChange,
                ErrorMessage = null
            };
        }

        /// <summary>
        /// Creates a failed validation result
        /// </summary>
        /// <param name="errorMessage">The error message</param>
        /// <returns>The <see cref="AuthValidationResult"/></returns>
        public static AuthValidationResult Fail(string errorMessage)
        {
            return new AuthValidationResult
            {
                Succeeded = false,
                ErrorMessage = errorMessage,
                User = null,
                RequiresPasswordChange = false
            };
        }
    }
}
