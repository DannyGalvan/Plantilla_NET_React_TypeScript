using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project.Migrations.MySql.Catalog
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "catalog_Categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    TenantId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Name = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    DeletedBy = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "char(36)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_Categories", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "catalog_OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    TenantId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Type = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false),
                    Payload = table.Column<string>(type: "longtext", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    LastError = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true),
                    LockId = table.Column<Guid>(type: "char(36)", nullable: true),
                    LockedUntil = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_OutboxMessages", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "catalog_Products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    TenantId = table.Column<Guid>(type: "char(36)", nullable: false),
                    OrganizationUnitId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Sku = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Stock = table.Column<int>(type: "int", nullable: false),
                    CategoryId = table.Column<Guid>(type: "char(36)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    DeletedBy = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "char(36)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_Products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_catalog_Products_catalog_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "catalog_Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_Categories_IsDeleted",
                table: "catalog_Categories",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_Categories_TenantId",
                table: "catalog_Categories",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_Categories_TenantId_Name",
                table: "catalog_Categories",
                columns: new[] { "TenantId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_catalog_OutboxMessages_ProcessedAt_OccurredAt",
                table: "catalog_OutboxMessages",
                columns: new[] { "ProcessedAt", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_catalog_Products_CategoryId",
                table: "catalog_Products",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_Products_IsDeleted",
                table: "catalog_Products",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_Products_OrganizationUnitId",
                table: "catalog_Products",
                column: "OrganizationUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_Products_TenantId",
                table: "catalog_Products",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_Products_TenantId_Name",
                table: "catalog_Products",
                columns: new[] { "TenantId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_catalog_Products_TenantId_Sku",
                table: "catalog_Products",
                columns: new[] { "TenantId", "Sku" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalog_OutboxMessages");

            migrationBuilder.DropTable(
                name: "catalog_Products");

            migrationBuilder.DropTable(
                name: "catalog_Categories");
        }
    }
}
