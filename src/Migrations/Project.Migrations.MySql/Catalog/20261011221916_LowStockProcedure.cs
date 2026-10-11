using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project.Migrations.MySql.Catalog
{
    /// <summary>Sample stored procedure used by the low-stock report (one implementation per engine).</summary>
    public partial class LowStockProcedure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE PROCEDURE `catalog_GetLowStockProducts`(IN p_TenantId char(36), IN p_Threshold int)
                BEGIN
                    SELECT `Id`, `Sku`, `Name`, `Stock`, `OrganizationUnitId`
                    FROM `catalog_Products`
                    WHERE `TenantId` = p_TenantId AND `IsDeleted` = FALSE AND `IsActive` = TRUE AND `Stock` < p_Threshold
                    ORDER BY `Stock`, `Name`;
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS `catalog_GetLowStockProducts`;");
        }
    }
}
