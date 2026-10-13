using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project.Migrations.SqlServer.Catalog
{
    /// <summary>Sample stored procedure used by the low-stock report (one implementation per engine).</summary>
    public partial class LowStockProcedure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE [catalog].[GetLowStockProducts]
                    @TenantId uniqueidentifier,
                    @Threshold int
                AS
                BEGIN
                    SET NOCOUNT ON;
                    SELECT [Id], [Sku], [Name], [Stock], [OrganizationUnitId]
                    FROM [catalog].[Products]
                    WHERE [TenantId] = @TenantId AND [IsDeleted] = 0 AND [IsActive] = 1 AND [Stock] < @Threshold
                    ORDER BY [Stock], [Name];
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [catalog].[GetLowStockProducts];");
        }
    }
}
