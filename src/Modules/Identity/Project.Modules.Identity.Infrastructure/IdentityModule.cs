using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Project.Application;
using Project.Application.Configuration;
using Project.Application.Security;
using Project.Application.Tenancy;
using Project.Infrastructure.Persistence;
using Project.Modules.Identity.Application;
using Project.Modules.Identity.Application.Auth;
using Project.Modules.Identity.Application.Roles;
using Project.Modules.Identity.Application.Users;
using Project.Modules.Identity.Infrastructure.Persistence;
using Project.Modules.Identity.Infrastructure.Presentation;
using Project.Modules.Identity.Infrastructure.Services;
using Project.Web.Modules;

namespace Project.Modules.Identity.Infrastructure;

/// <summary>
/// Authentication (JWT + rotating refresh tokens), users, roles, permissions and tenant memberships.
/// Platform module: users are global and may belong to several tenants.
/// </summary>
public sealed class IdentityModule : IModule
{
    public string Name => "Identity";

    public DataScope DataScope => DataScope.Platform;

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<IdentityOptions>().Bind(configuration.GetSection(IdentityOptions.SectionName));
        services.AddConfigurableSection<IdentityOptions>(IdentityOptions.SectionName, "identity", "URL pública para enlaces de correo y vigencia del restablecimiento de contraseña.");

        services.AddModuleDbContext<IdentityDbContext>(Name, DataScope);
        services.AddRepositoriesFor<IdentityDbContext>();
        services.AddModuleConventions(typeof(IdentityPermissionProvider).Assembly, typeof(IdentityModule).Assembly);

        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<ISecureTokenGenerator, SecureTokenGenerator>();
        services.AddSingleton<PermissionCatalog>();

        services.AddScoped<IIdentityReadService, IdentityReadService>();
        services.AddScoped<Contracts.IUserDirectory, UserDirectory>();
        services.AddScoped<PermissionService>();
        services.AddScoped<IPermissionService>(sp => sp.GetRequiredService<PermissionService>());
        services.AddScoped<IPermissionCache>(sp => sp.GetRequiredService<PermissionService>());
        services.AddScoped<IPermissionSynchronizer, PermissionSynchronizer>();

        services.AddScoped<SessionIssuer>();
        services.AddScoped<PasswordPolicy>();
        services.AddScoped<PasswordChanger>();
        services.AddScoped<UserScope>();
        services.AddScoped<RoleScope>();

        services.AddScoped<IDataSeeder, IdentityPlatformSeeder>();
        services.AddScoped<IDataSeeder, IdentityTenantSeeder>();
    }

    public void MapEndpoints(IEndpointRouteBuilder api)
    {
        AuthEndpoints.Map(api);
        AdminEndpoints.Map(api);
    }
}
