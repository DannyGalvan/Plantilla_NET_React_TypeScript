namespace Project.Server.Configs.Extensions
{
    /// <summary>
    /// Defines the <see cref="SessionSecurityConfiguration" />
    /// </summary>
    public static class SessionSecurityConfiguration
    {
        /// <summary>
        /// Adds session security configuration
        /// </summary>
        /// <param name="services">The services<see cref="IServiceCollection"/></param>
        /// <returns>The <see cref="IServiceCollection"/></returns>
        public static IServiceCollection AddSessionSecurityConfiguration(this IServiceCollection services)
        {
            // Configure session
            services.AddDistributedMemoryCache();
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Strict;
            });

            return services;
        }
    }
}
