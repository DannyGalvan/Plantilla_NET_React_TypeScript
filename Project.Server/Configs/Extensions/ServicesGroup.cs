using Project.Server.Entities.Models;
using Project.Server.Entities.Request;
using Project.Server.Interceptors.Interfaces;
using Project.Server.Interceptors.UserInterceptors;
using Project.Server.Security;
using Project.Server.Services.Background;
using Project.Server.Services.Core;
using Project.Server.Services.Interfaces;
using Project.Server.Utils;
using AuthService = Project.Server.Services.Core.AuthService;
using SendEmail = Project.Server.Services.Core.SendEmail;


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
            services.AddScoped<IEntityService<Operation, OperationRequest, long>, EntityService<Operation, OperationRequest, long>>();
            services.AddScoped<IEntityService<RolOperation, RolOperationRequest, long>, EntityService<RolOperation, RolOperationRequest, long>>();

            // User interceptors
            services.AddScoped<IEntityBeforeCreateInterceptor<User, UserRequest>, UserBeforeCreateInterceptor>();
            services.AddScoped<IEntityBeforeUpdateInterceptor<User, UserRequest>, UserBeforeUpdateInterceptor>();

            // security services
            services.AddScoped<ISecurityAuthService, SecurityAuthService>();
            // SessionAuthService and SessionAuthorizationFilter removed - using JWT authorization
            // services.AddScoped<SessionAuthService>();
            // services.AddScoped<SessionAuthorizationFilter>();

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
