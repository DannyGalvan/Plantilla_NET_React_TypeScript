namespace Project.Modules.Identity.Contracts;

public sealed record UserSummary(Guid Id, string UserName, string? DisplayName, string Email);

/// <summary>Public read API of Identity for other modules (they never touch Identity tables directly).</summary>
public interface IUserDirectory
{
    /// <summary>Users of the current tenant with the given ids. Unknown ids are skipped.</summary>
    Task<IReadOnlyList<UserSummary>> FindAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct);

    /// <summary>True when the user exists and belongs to the current tenant.</summary>
    Task<bool> IsMemberAsync(Guid userId, CancellationToken ct);

    /// <summary>Members of the current tenant holding <paramref name="permission"/> through a role (approvers, recipients).</summary>
    Task<IReadOnlyList<UserSummary>> FindWithPermissionAsync(string permission, CancellationToken ct);

    /// <summary>Members of the current tenant assigned to the role named <paramref name="role"/>.</summary>
    Task<IReadOnlyList<UserSummary>> FindInRoleAsync(string role, CancellationToken ct);
}
