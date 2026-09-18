using Microsoft.Extensions.Options;
using Project.Server.Configs.Models;
using Project.Server.Entities.Models;
using Project.Server.Entities.Request;
using Project.Server.Interceptors.Interfaces;
using Project.Server.Interceptors.UserInterceptors;
using Project.Server.Services.Background;
using Project.Server.Services.Core;
using Project.Server.Services.Interfaces;
using Project.Server.Utils;
using AuthService = Project.Server.Services.Core.AuthService;
using SendEmail = Project.Server.Services.Core.SendEmail;

namespace Project.Server.Configs.Extensions
{
    public static class ServicesGroup
    {
        public static IServiceCollection AddServiceGroup(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();

            // CRUD services — Phase 1 EntityService is async, ownership-aware, allowlist-bound.
            services.AddScoped<IEntityService<User, UserRequest, long>, EntityService<User, UserRequest, long>>();
            services.AddScoped<IEntityService<Rol, RolRequest, long>, EntityService<Rol, RolRequest, long>>();
            services.AddScoped<IEntityService<Operation, OperationRequest, long>, EntityService<Operation, OperationRequest, long>>();
            services.AddScoped<IEntityService<RolOperation, RolOperationRequest, long>, EntityService<RolOperation, RolOperationRequest, long>>();

            // QueryPolicy<TEntity> is open generic — DI resolves the closed type per service.
            services.AddTransient(typeof(QueryPolicy<>));

            services.AddSingleton<ISortTranslator, SortTranslator>();
            services.AddScoped<IFilterTranslator, FilterTranslator>();

            // Single source of truth for password complexity (B7).
            services.AddSingleton<SecurityPasswordPolicy>();

            // Pagination bounds come from configuration and are validated on startup.
            services.AddOptions<PaginationOptions>()
                .BindConfiguration(PaginationOptions.SectionName)
                .ValidateOnStart();
            services.AddSingleton(sp => sp.GetRequiredService<IOptions<PaginationOptions>>().Value);

            // User interceptors (existing; shape unchanged).
            services.AddScoped<IEntityBeforeCreateInterceptor<User, UserRequest>, UserBeforeCreateInterceptor>();
            services.AddScoped<IEntityBeforeUpdateInterceptor<User, UserRequest>, UserBeforeUpdateInterceptor>();

            // Phase 3: SecurityAuthService merged into AuthService — the dead
            // registration is gone.

            // operation sync + hosted bootstrap
            services.AddScoped<IOperationSyncService, OperationSyncService>();
            services.AddHostedService<OperationSyncHostedService>();

            // Ownership validation refuses to boot when an entity exposed through
            // the generic stack does not implement an ownership contract.
            services.AddHostedService<OwnershipValidationHostedService>();

            // Bootstrap the SA seed user with a real password (B6).
            services.AddHostedService<AdminSeedHostedService>();

            // other services
            services.AddScoped<ISendMail, SendEmail>();
            services.AddScoped<IEntitySupportService, EntitySupportService>();
            services.AddScoped<IAuthService, AuthService>();

            return services;
        }
    }
}