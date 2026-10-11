using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project.Migrations.MySql.Organization
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
                name: "organization_OutboxMessages",
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
                    table.PrimaryKey("PK_organization_OutboxMessages", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "organization_UnitAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    TenantId = table.Column<Guid>(type: "char(36)", nullable: false),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false),
                    UnitId = table.Column<Guid>(type: "char(36)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization_UnitAssignments", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "organization_Units",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    TenantId = table.Column<Guid>(type: "char(36)", nullable: false),
                    TypeCode = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    ParentId = table.Column<Guid>(type: "char(36)", nullable: true),
                    Path = table.Column<string>(type: "varchar(700)", maxLength: 700, nullable: false),
                    Depth = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    CountryCode = table.Column<string>(type: "varchar(2)", maxLength: 2, nullable: true),
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
                    table.PrimaryKey("PK_organization_Units", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "organization_UnitTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    TenantId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Code = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    CanBeRoot = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AllowedParentCodes = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_organization_UnitTypes", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_organization_OutboxMessages_ProcessedAt_OccurredAt",
                table: "organization_OutboxMessages",
                columns: new[] { "ProcessedAt", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_organization_UnitAssignments_TenantId",
                table: "organization_UnitAssignments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_organization_UnitAssignments_TenantId_UserId_UnitId",
                table: "organization_UnitAssignments",
                columns: new[] { "TenantId", "UserId", "UnitId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_organization_UnitAssignments_UnitId",
                table: "organization_UnitAssignments",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_organization_Units_IsDeleted",
                table: "organization_Units",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_organization_Units_ParentId",
                table: "organization_Units",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_organization_Units_TenantId",
                table: "organization_Units",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_organization_Units_TenantId_Code",
                table: "organization_Units",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_organization_Units_TenantId_Path",
                table: "organization_Units",
                columns: new[] { "TenantId", "Path" });

            migrationBuilder.CreateIndex(
                name: "IX_organization_UnitTypes_IsDeleted",
                table: "organization_UnitTypes",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_organization_UnitTypes_TenantId",
                table: "organization_UnitTypes",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_organization_UnitTypes_TenantId_Code",
                table: "organization_UnitTypes",
                columns: new[] { "TenantId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "organization_OutboxMessages");

            migrationBuilder.DropTable(
                name: "organization_UnitAssignments");

            migrationBuilder.DropTable(
                name: "organization_Units");

            migrationBuilder.DropTable(
                name: "organization_UnitTypes");
        }
    }
}
