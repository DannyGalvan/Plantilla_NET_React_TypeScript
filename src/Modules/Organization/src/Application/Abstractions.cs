namespace Project.Modules.Organization.Application;

/// <summary>
/// Source of truth for the current organization scope. Backed by HybridCache
/// keyed by \`(tid, userId, ou, pv)\`. Returns NoOp when the Organization
/// module is not registered.
/// </summary>
public interface IOrganizationCatalog
{
    ValueTask<Guid?> GetActiveUnitIdAsync(Guid? tenantId, Guid userId, Guid? requestedUnitId, CancellationToken ct);
    ValueTask<IReadOnlySet<Guid>> GetAccessibleUnitIdsAsync(Guid? tenantId, Guid userId, long permissionsVersion, Guid? requestedUnitId, CancellationToken ct);
    Task InvalidateAsync(Guid userId, CancellationToken ct);
}

public interface IOrganizationManager
{
    Task<Guid> CreateAsync(CreateOrganizationUnitCommand command, CancellationToken ct);
    Task MoveAsync(Guid unitId, Guid? newParentId, CancellationToken ct);
    Task AssignUserToUnitAsync(Guid userId, Guid unitId, CancellationToken ct);
}

public sealed record CreateOrganizationUnitCommand(
    Guid TenantId,
    string TypeCode,
    Guid? ParentId,
    string Code,
    string Name,
    string? CountryCode);