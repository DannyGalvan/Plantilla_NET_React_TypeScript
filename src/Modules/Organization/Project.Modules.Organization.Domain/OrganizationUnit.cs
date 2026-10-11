using Project.SharedKernel;
using Project.SharedKernel.Domain;

namespace Project.Modules.Organization.Domain;

/// <summary>
/// Kind of unit (Company, Country, Brand, Branch, Region, Warehouse...). Configurable per tenant without code:
/// <see cref="AllowedParentCodes"/> lists the types that may contain it (comma separated).
/// </summary>
public sealed class OrganizationUnitType : AuditableAggregateRoot, IMultiTenant
{
    public Guid TenantId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>Whether a unit of this type may be a root (no parent).</summary>
    public bool CanBeRoot { get; set; }

    /// <summary>Comma separated codes of the parent types allowed, for example <c>company,country</c>.</summary>
    public string AllowedParentCodes { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool AllowsParent(string? parentTypeCode) =>
        parentTypeCode is null
            ? CanBeRoot
            : AllowedParentCodes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Contains(parentTypeCode, StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Node of the organization tree inside a tenant. <see cref="Path"/> materializes the hierarchy
/// (<c>/{rootId}/{childId}/</c>) so descendants are found with an indexed prefix search on every engine.
/// </summary>
public sealed class OrganizationUnit : AuditableAggregateRoot, IMultiTenant
{
    private OrganizationUnit()
    {
    }

    public Guid TenantId { get; set; }

    public string TypeCode { get; private set; } = string.Empty;

    public Guid? ParentId { get; private set; }

    public string Path { get; private set; } = string.Empty;

    public int Depth { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string? CountryCode { get; private set; }

    public bool IsActive { get; private set; } = true;

    public static string Segment(Guid id) => $"{id:N}/";

    /// <summary>Ids from the root to this unit, parsed from <see cref="Path"/>.</summary>
    public static IReadOnlyList<Guid> ParsePath(string path) =>
        path.Split("/", StringSplitOptions.RemoveEmptyEntries).Select(s => Guid.ParseExact(s, "N")).ToList();

    public static Result<OrganizationUnit> Create(OrganizationUnitType type, OrganizationUnit? parent, string code, string name, string? countryCode)
    {
        if (!type.AllowsParent(parent?.TypeCode))
        {
            return Error.Validation("parentId", parent is null
                ? $"A '{type.Name}' cannot be a root unit."
                : $"A '{type.Name}' cannot be placed under a '{parent.TypeCode}'.");
        }

        var unit = new OrganizationUnit
        {
            TypeCode = type.Code,
            ParentId = parent?.Id,
            Code = code.Trim(),
            Name = name.Trim(),
            CountryCode = countryCode?.Trim().ToUpperInvariant(),
        };
        unit.Path = (parent?.Path ?? "/") + Segment(unit.Id);
        unit.Depth = parent is null ? 0 : parent.Depth + 1;
        return unit;
    }

    public void Update(string code, string name, string? countryCode, bool isActive)
    {
        Code = code.Trim();
        Name = name.Trim();
        CountryCode = countryCode?.Trim().ToUpperInvariant();
        IsActive = isActive;
    }

    /// <summary>Moves the unit (the caller rewrites the paths of the descendants with <see cref="RebasePath"/>).</summary>
    public Result MoveTo(OrganizationUnitType type, OrganizationUnit? newParent)
    {
        if (newParent is not null && newParent.Path.StartsWith(Path, StringComparison.Ordinal))
        {
            return Error.Validation("parentId", "A unit cannot be moved under itself or its descendants.");
        }

        if (!type.AllowsParent(newParent?.TypeCode))
        {
            return Error.Validation("parentId", $"A '{type.Name}' cannot be placed there.");
        }

        ParentId = newParent?.Id;
        Path = (newParent?.Path ?? "/") + Segment(Id);
        Depth = newParent is null ? 0 : newParent.Depth + 1;
        return Result.Success();
    }

    /// <summary>Replaces the path prefix of a descendant after its ancestor moved.</summary>
    public void RebasePath(string oldPrefix, string newPrefix, int depthDelta)
    {
        Path = newPrefix + Path[oldPrefix.Length..];
        Depth += depthDelta;
    }
}

/// <summary>A user works in a unit; the user sees that unit and its descendants.</summary>
public sealed class UnitAssignment : AggregateRoot<Guid>, IMultiTenant
{
    private UnitAssignment()
    {
    }

    public Guid TenantId { get; set; }

    public Guid UserId { get; private set; }

    public Guid UnitId { get; private set; }

    public static UnitAssignment Create(Guid userId, Guid unitId) => new()
    {
        Id = NewId.Next(),
        UserId = userId,
        UnitId = unitId,
    };
}
