using Project.Server.Security;
using Project.Server.Services.Background;
using Project.Server.Services.Core;
using Project.Server.Services.Interfaces;
using SoluEmpleo.Server.Services.Core;


namespace Project.Server.Configs.Extensions
{

    /// <summary>
    /// Defines the <see cref="ServicesGroup" />
    /// </summary>
    public static class ServicesGroup
    {
        /// <summary>
        /// The AddServiceGroup
        /// </summary>
        /// <param name="services">The services<see cref="IServiceCollection"/></param>
        /// <returns>The <see cref="IServiceCollection"/></returns>
        public static IServiceCollection AddServiceGroup(this IServiceCollection services)
        {
            // entities services
            services.AddScoped<IAuthService, AuthService>();

            // security services
            services.AddScoped<ISecurityAuthService, SecurityAuthService>();
            services.AddScoped<SessionAuthService>();
            services.AddScoped<SessionAuthorizationFilter>();

            // operation sync service
            services.AddScoped<IOperationSyncService, OperationSyncService>();

            // hosted service para sincronización al inicio
            services.AddHostedService<OperationSyncHostedService>();

            // other services
            services.AddScoped<ISendMail, SendEmail>();

            return services;
        }
    }
}
