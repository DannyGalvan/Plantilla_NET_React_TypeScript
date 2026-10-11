namespace Project.Application.Abstractions;

/// <summary>
/// Current request principal resolved by middleware. Scoped.
/// </summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    string? UserId { get; }
    Guid? TenantId { get; }
    Guid? ActiveOrganizationUnitId { get; }
    IReadOnlyCollection<string> Roles { get; }
}