namespace Project.Application.Abstractions;

/// <summary>
/// Principal of the current request (or the system account in background work). Scoped.
/// </summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    /// <summary>Subject id (<c>sub</c> claim). Null for anonymous requests.</summary>
    string? UserId { get; }

    string? UserName { get; }

    /// <summary>Active tenant from the <c>tid</c> claim.</summary>
    Guid? TenantId { get; }

    /// <summary>Active organization unit from the <c>ou</c> claim.</summary>
    Guid? ActiveOrganizationUnitId { get; }

    /// <summary>Permissions version from the <c>pv</c> claim; used as part of the permission cache key.</summary>
    long PermissionsVersion { get; }

    IReadOnlyCollection<string> Roles { get; }

    string? IpAddress { get; }

    string? UserAgent { get; }
}
