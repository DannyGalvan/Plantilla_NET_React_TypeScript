using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Project.Modules.Organization.Application;
using Project.Web;

namespace Project.Modules.Organization;

/// <summary>
/// Organization module. Optional: if not registered, the global filter for
/// \`IOrganizationScoped\` is skipped (\`IOrganizationContext.Enabled=false\`).
/// </summary>
public sealed class OrganizationModule : IModule
{
    public string Name => "Organization";
    public DataScope DataScope => DataScope.Tenant;

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IOrganizationCatalog, NullOrganizationCatalog>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        // Phase 4B ships the catalog abstraction. MapCrud for OrganizationUnit
        // + Move/AssignUserToUnit endpoints land with the DbContext (Fase 2
        // next slice).
    }
}

internal sealed class NullOrganizationCatalog : IOrganizationCatalog
{
    public ValueTask<Guid?> GetActiveUnitIdAsync(Guid? tenantId, Guid userId, Guid? requestedUnitId, CancellationToken ct) =>
        ValueTask.FromResult<Guid?>(null);

    public ValueTask<IReadOnlySet<Guid>> GetAccessibleUnitIdsAsync(Guid? tenantId, Guid userId, long permissionsVersion, Guid? requestedUnitId, CancellationToken ct) =>
        ValueTask.FromResult<IReadOnlySet<Guid>>(new HashSet<Guid>());

    public Task InvalidateAsync(Guid userId, CancellationToken ct) => Task.CompletedTask;
}