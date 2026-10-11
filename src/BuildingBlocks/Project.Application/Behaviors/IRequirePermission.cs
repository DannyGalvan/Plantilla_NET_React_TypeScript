namespace Project.Application.Behaviors;

/// <summary>
/// Marker for requests that require a permission check (AuthorizationBehavior).
/// </summary>
public interface IRequirePermission
{
    /// <summary>Permission key in <c>{module}.{resource}.{action}</c> form.</summary>
    string Permission { get; }
}