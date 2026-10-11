using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Project.Application;
using Project.Application.Abstractions;
using Project.Modules.Identity.Application;
using Project.Modules.Identity.Application.Auth;
using Project.Modules.Identity.Infrastructure;
using Project.Modules.Identity.Infrastructure.Security;
using Project.Web;

namespace Project.Modules.Identity;

/// <summary>
/// Identity module. \`DataScope=Platform\` so the catalog lives in the
/// platform connection string. Wires the token issuer, refresh service,
/// password services, lockout policy, permission cache and authorization
/// handler.
/// </summary>
public sealed class IdentityModule : IModule
{
    public string Name => "Identity";
    public DataScope DataScope => DataScope.Platform;

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
        services.AddSingleton(sp => sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<JwtSettings>>().Value);

        services.AddSingleton<ITokenIssuer, JwtTokenIssuer>();
        services.AddSingleton<IRefreshTokenService, Sha256RefreshTokenService>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IPasswordPolicy, DefaultPasswordPolicy>();
        services.AddSingleton<ILockoutPolicy, DefaultLockoutPolicy>();
        services.AddScoped<IPasswordHistoryChecker, NullPasswordHistoryChecker>();
        services.AddScoped<ILoginAuditWriter, NullLoginAuditWriter>();
        services.AddSingleton<TimeProvider>(System.TimeProvider.System);

        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddSingleton<IPermissionService, HybridCachePermissionService>();
        services.AddSingleton<IPermissionCatalog, NullPermissionCatalog>();

        services.AddScoped<LoginCommandHandler>();
        services.AddScoped<RefreshTokenCommandHandler>();
        services.AddScoped<ChangePasswordCommandHandler>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var auth = endpoints.MapGroup("/api/v1/identity/auth");
        auth.MapPost("/login", async (LoginCommand cmd, IMediator mediator, HttpContext http) =>
            (await mediator.Send(cmd)).ToHttp(http));
        auth.MapPost("/refresh", async (RefreshTokenCommand cmd, IMediator mediator, HttpContext http) =>
            (await mediator.Send(cmd)).ToHttp(http));
        auth.MapPost("/change-password", async (ChangePasswordCommand cmd, IMediator mediator, HttpContext http) =>
            (await mediator.Send(cmd)).ToHttp(http));
    }
}

internal sealed class NullPasswordHistoryChecker : IPasswordHistoryChecker
{
    public Task<bool> HasBeenUsedRecentlyAsync(Guid userId, string newPassword, int limit, CancellationToken ct) =>
        Task.FromResult(false);
}

internal sealed class NullLoginAuditWriter : ILoginAuditWriter
{
    public Task WriteAsync(LoginAuditDescriptor descriptor, CancellationToken ct) => Task.CompletedTask;
}

internal sealed class NullPermissionCatalog : IPermissionCatalog
{
    public Task<HashSet<string>> GetPermissionsForUserAsync(Guid userId, CancellationToken ct) =>
        Task.FromResult<HashSet<string>>([]);
}