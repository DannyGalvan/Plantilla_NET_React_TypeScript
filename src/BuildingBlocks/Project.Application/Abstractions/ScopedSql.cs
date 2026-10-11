using Project.Application.Organization;
using Project.Application.Tenancy;

namespace Project.Application.Abstractions;

/// <summary>
/// Helpers that keep raw SQL inside the tenant and organization scope. Global EF filters do not apply to Dapper,
/// so every query over tenant data must use them:
/// <code>
/// var scope = ScopedSql.For(executor.Dialect, tenant, organization);
/// var sql = $"SELECT ... FROM {table} p WHERE {scope.Tenant("p")} AND {scope.Units("p")}";
/// await executor.QueryAsync&lt;Row&gt;(sql, scope.Parameters(new { Threshold = 5 }), ct);
/// </code>
/// </summary>
public sealed class ScopedSql
{
    private readonly ISqlDialect _dialect;
    private readonly Guid _tenantId;
    private readonly IOrganizationContext _organization;

    private ScopedSql(ISqlDialect dialect, Guid tenantId, IOrganizationContext organization)
    {
        _dialect = dialect;
        _tenantId = tenantId;
        _organization = organization;
    }

    public static ScopedSql For(ISqlDialect dialect, ITenantContext tenant, IOrganizationContext organization) =>
        new(dialect, tenant.TenantId ?? TenantDefaults.DefaultTenantId, organization);

    /// <summary><c>alias.TenantId = @TenantId</c>. Always applied: in dedicated databases it is a no-op filter.</summary>
    public string Tenant(string alias) => $"{alias}.{_dialect.QuoteIdentifier("TenantId")} = @TenantId";

    /// <summary><c>alias.OrganizationUnitId IN @UnitIds</c>, or <c>1 = 1</c> when the caller is not restricted.</summary>
    public string Units(string alias) =>
        IsUnitFiltered ? $"{alias}.{_dialect.QuoteIdentifier("OrganizationUnitId")} IN @UnitIds" : "1 = 1";

    /// <summary><c>alias.IsDeleted = FALSE</c>.</summary>
    public string NotDeleted(string alias) => $"{alias}.{_dialect.QuoteIdentifier("IsDeleted")} = {_dialect.Boolean(false)}";

    public bool IsUnitFiltered => _organization.Enabled && !_organization.HasFullAccess;

    /// <summary>Merges <c>@TenantId</c> and <c>@UnitIds</c> with the query parameters.</summary>
    public Dictionary<string, object?> Parameters(object? extra = null)
    {
        var parameters = new Dictionary<string, object?> { ["TenantId"] = _tenantId };
        if (IsUnitFiltered)
        {
            parameters["UnitIds"] = _organization.AccessibleUnitIds.ToArray();
        }

        foreach (var property in extra?.GetType().GetProperties() ?? [])
        {
            parameters[property.Name] = property.GetValue(extra);
        }

        return parameters;
    }

    /// <summary>Quoted column name.</summary>
    public string Column(string alias, string column) => $"{alias}.{_dialect.QuoteIdentifier(column)}";
}
