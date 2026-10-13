using Project.Application.Abstractions;
using Project.Application.Messaging;
using Project.Application.Organization;
using Project.Application.Tenancy;
using Project.SharedKernel;

namespace Project.Modules.Catalog.Application;

/// <summary>Marker that selects the Catalog database for <see cref="ISqlExecutor{TModule}"/>.</summary>
public sealed class CatalogDb;

/// <summary>Physical names used by raw SQL (kept next to the queries that use them).</summary>
public static class CatalogTables
{
    public const string Schema = "catalog";
    public const string Products = "Products";
    public const string Categories = "Categories";
    public const string LowStockProcedure = "GetLowStockProducts";
}

/// <summary>Row mapped by Dapper (settable properties tolerate numeric type differences between engines).</summary>
public sealed class StockReportRow
{
    public Guid CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public long Products { get; set; }

    public long UnitsInStock { get; set; }

    public decimal InventoryValue { get; set; }
}

/// <summary>Inventory per category with Dapper: a high-performance read that bypasses EF.</summary>
public sealed record GetStockReportQuery : IQuery<IReadOnlyList<StockReportRow>>;

public sealed class GetStockReportHandler(ISqlExecutor<CatalogDb> sql, ITenantContext tenant, IOrganizationContext organization)
    : IQueryHandler<GetStockReportQuery, IReadOnlyList<StockReportRow>>
{
    public async ValueTask<Result<IReadOnlyList<StockReportRow>>> Handle(GetStockReportQuery query, CancellationToken cancellationToken)
    {
        var d = sql.Dialect;
        var scope = ScopedSql.For(d, tenant, organization);
        var text = $"""
            SELECT {scope.Column("c", "Id")} AS CategoryId,
                   {scope.Column("c", "Name")} AS CategoryName,
                   COUNT({scope.Column("p", "Id")}) AS Products,
                   COALESCE(SUM({scope.Column("p", "Stock")}), 0) AS UnitsInStock,
                   COALESCE(SUM({scope.Column("p", "Price")} * {scope.Column("p", "Stock")}), 0) AS InventoryValue
            FROM {d.Table(CatalogTables.Schema, CatalogTables.Categories)} c
            LEFT JOIN {d.Table(CatalogTables.Schema, CatalogTables.Products)} p
                ON {scope.Column("p", "CategoryId")} = {scope.Column("c", "Id")}
               AND {scope.Tenant("p")} AND {scope.NotDeleted("p")} AND {scope.Units("p")}
            WHERE {scope.Tenant("c")} AND {scope.NotDeleted("c")}
            GROUP BY {scope.Column("c", "Id")}, {scope.Column("c", "Name")}
            ORDER BY {scope.Column("c", "Name")}
            """;

        var rows = await sql.QueryAsync<StockReportRow>(text, scope.Parameters(), cancellationToken);
        return Result<IReadOnlyList<StockReportRow>>.Success(rows);
    }
}

public sealed class LowStockRow
{
    public Guid Id { get; set; }

    public string Sku { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int Stock { get; set; }

    public Guid OrganizationUnitId { get; set; }
}

/// <summary>Products below a stock threshold, served by a stored procedure created by the migrations of each engine.</summary>
public sealed record GetLowStockQuery(int Threshold) : IQuery<IReadOnlyList<LowStockRow>>;

public sealed class GetLowStockHandler(ISqlExecutor<CatalogDb> sql, ITenantContext tenant, IOrganizationContext organization)
    : IQueryHandler<GetLowStockQuery, IReadOnlyList<LowStockRow>>
{
    public async ValueTask<Result<IReadOnlyList<LowStockRow>>> Handle(GetLowStockQuery query, CancellationToken cancellationToken)
    {
        if (query.Threshold is < 0 or > 1_000_000)
        {
            return Error.Validation("threshold", "The threshold must be between 0 and 1000000.");
        }

        var rows = await sql.QueryProcedureAsync<LowStockRow>(
            CatalogTables.LowStockProcedure,
            new { TenantId = tenant.TenantId ?? TenantDefaults.DefaultTenantId, Threshold = query.Threshold },
            cancellationToken);

        // The procedure filters by tenant; the organization scope is applied here.
        IReadOnlyList<LowStockRow> visible = organization.Enabled && !organization.HasFullAccess
            ? rows.Where(r => organization.AccessibleUnitIds.Contains(r.OrganizationUnitId)).ToList()
            : rows;
        return Result<IReadOnlyList<LowStockRow>>.Success(visible);
    }
}
