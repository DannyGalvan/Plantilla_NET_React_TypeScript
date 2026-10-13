using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project.Migrations.PostgreSql.Catalog
{
    /// <summary>Sample stored procedure used by the low-stock report (one implementation per engine).</summary>
    public partial class LowStockProcedure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION catalog."GetLowStockProducts"("TenantId" uuid, "Threshold" integer)
                RETURNS TABLE("Id" uuid, "Sku" varchar, "Name" varchar, "Stock" integer, "OrganizationUnitId" uuid)
                LANGUAGE sql STABLE
                AS $$
                    SELECT p."Id", p."Sku", p."Name", p."Stock", p."OrganizationUnitId"
                    FROM catalog."Products" p
                    WHERE p."TenantId" = $1 AND NOT p."IsDeleted" AND p."IsActive" AND p."Stock" < $2
                    ORDER BY p."Stock", p."Name";
                $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS catalog.\"GetLowStockProducts\"(uuid, integer);");
        }
    }
}
