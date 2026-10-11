using Microsoft.AspNetCore.Routing;

namespace Project.Web;

/// <summary>
/// Scope of a module's data: Platform (catalog/identity/tenancy) or Tenant (business).
/// </summary>
public enum DataScope
{
    Platform,
    Tenant,
}

/// <summary>
/// A backend module. Each module owns its services, endpoints and DbContexts.
/// </summary>
public interface IModule
{
    string Name { get; }
    DataScope DataScope { get; }

    void AddServices(IServiceCollection services, IConfiguration configuration);

    void MapEndpoints(IEndpointRouteBuilder endpoints);
}