namespace Project.Server.Security
{
    /// <summary>
    /// Defines the <see cref="SessionKeys" />
    /// </summary>
    public static class SessionKeys
    {
        /// <summary>
        /// Session key for UserId
        /// </summary>
        public const string UserId = "Security.UserId";

        /// <summary>
        /// Session key for UserName
        /// </summary>
        public const string UserName = "Security.UserName";

        /// <summary>
        /// Session key for FullName
        /// </summary>
        public const string FullName = "Security.FullName";

        /// <summary>
        /// Session key for RoleId
        /// </summary>
        public const string RoleId = "Security.RoleId";

        /// <summary>
        /// Session key for RoleName
        /// </summary>
        public const string RoleName = "Security.RoleName";

        /// <summary>
        /// Session key for AllowedOperations
        /// </summary>
        public const string AllowedOperations = "Security.AllowedOperations";

        /// <summary>
        /// Session key for MustChangePassword
        /// </summary>
        public const string MustChangePassword = "Security.MustChangePassword";
    }
}
