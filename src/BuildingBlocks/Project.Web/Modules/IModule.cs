using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Project.Application.Tenancy;

namespace Project.Web.Modules;

/// <summary>
/// A vertical slice of the application (Identity, Catalog, Inventory...). Adding a module is one line in the host:
/// <c>builder.AddModules(new InventoryModule())</c>.
/// </summary>
public interface IModule
{
    /// <summary>Module name. Also the URL segment (lower case) and the database schema.</summary>
    string Name { get; }

    DataScope DataScope { get; }

    void AddServices(IServiceCollection services, IConfiguration configuration);

    /// <summary>Maps the module endpoints. <paramref name="api"/> is already prefixed with <c>/api/v1</c>.</summary>
    void MapEndpoints(IEndpointRouteBuilder api);
}

/// <summary>Modules that were loaded (disabled modules are excluded).</summary>
public sealed class ModuleCatalog(IReadOnlyList<IModule> modules)
{
    public IReadOnlyList<IModule> Modules { get; } = modules;

    public bool IsLoaded(string name) => Modules.Any(m => string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase));
}

public static class ModuleLoader
{
    public const string ApiPrefix = "/api/v1";

    /// <summary>
    /// Registers the modules. A module listed in <c>Modules:Disabled</c> is skipped entirely (services and endpoints).
    /// </summary>
    public static IHostApplicationBuilder AddModules(this IHostApplicationBuilder builder, params IModule[] modules)
    {
        var disabled = builder.Configuration.GetSection("Modules:Disabled").Get<string[]>() ?? [];
        var enabled = modules
            .Where(m => !disabled.Contains(m.Name, StringComparer.OrdinalIgnoreCase))
            .ToList();

        var duplicated = enabled.GroupBy(m => m.Name, StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);
        if (duplicated is not null)
        {
            throw new InvalidOperationException($"Module '{duplicated.Key}' is registered more than once.");
        }

        foreach (var module in enabled)
        {
            module.AddServices(builder.Services, builder.Configuration);
        }

        builder.Services.AddSingleton(new ModuleCatalog(enabled));
        return builder;
    }

    /// <summary>Maps every loaded module under <c>/api/v1</c>.</summary>
    public static WebApplication MapModules(this WebApplication app)
    {
        var api = app.MapGroup(ApiPrefix);
        foreach (var module in app.Services.GetRequiredService<ModuleCatalog>().Modules)
        {
            module.MapEndpoints(api);
        }

        return app;
    }
}
