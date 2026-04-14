using Project.Server.Entities.Models;
using Project.Server.Entities.Request;
using Project.Server.Security;
using Project.Server.Services.Background;
using Project.Server.Services.Core;
using Project.Server.Services.Interfaces;
using Project.Server.Utils;
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

            // CRUD services
            services.AddScoped<IEntityService<User, UserRequest, long>, EntityService<User, UserRequest, long>>();
            services.AddScoped<IEntityService<Rol, RolRequest, long>, EntityService<Rol, RolRequest, long>>();

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

            services.AddScoped<IFilterTranslator, FilterTranslator>();
            services.AddScoped<IEntitySupportService, EntitySupportService>();

            return services;
        }
    }
}
