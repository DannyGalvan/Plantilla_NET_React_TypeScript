namespace Project.Application.Organization;

/// <summary>
/// Organization scope of the current request. When the Organization module is not registered,
/// a no-op implementation reports <see cref="Enabled"/> = false and no filter is applied.
/// </summary>
public interface IOrganizationContext
{
    bool Enabled { get; }

    /// <summary>Active unit (<c>ou</c> claim), if any.</summary>
    Guid? ActiveUnitId { get; }

    /// <summary>True when the user can see every unit of the tenant (<c>organization.scope.all</c>).</summary>
    bool HasFullAccess { get; }

    /// <summary>Active unit plus its descendants that the user is allowed to see.</summary>
    IReadOnlySet<Guid> AccessibleUnitIds { get; }

    /// <summary>Active unit and its ancestors, root first. Used by cascading settings and role inheritance.</summary>
    IReadOnlyList<Guid> ActiveUnitPath { get; }

    /// <summary>Loads the scope for the current principal. Idempotent; called once per request.</summary>
    ValueTask InitializeAsync(CancellationToken ct);
}

/// <summary>Contract the Organization module exposes to the rest of the platform.</summary>
public interface IOrganizationDirectory
{
    /// <summary>Returns the unit and its ancestors (root first), or an empty list if it does not exist.</summary>
    Task<IReadOnlyList<Guid>> GetPathAsync(Guid unitId, CancellationToken ct);

    /// <summary>Ids of the units assigned to the user in the current tenant.</summary>
    Task<IReadOnlyList<Guid>> GetAssignedUnitsAsync(string userId, CancellationToken ct);

    /// <summary>Descendants of the given units, including themselves.</summary>
    Task<IReadOnlySet<Guid>> ExpandDescendantsAsync(IReadOnlyCollection<Guid> unitIds, CancellationToken ct);
}
