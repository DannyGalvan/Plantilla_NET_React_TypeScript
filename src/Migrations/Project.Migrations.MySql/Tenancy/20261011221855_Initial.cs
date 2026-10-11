using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project.Migrations.MySql.Tenancy
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
                name: "tenancy_OutboxMessages",
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
                    table.PrimaryKey("PK_tenancy_OutboxMessages", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "tenancy_SettingValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    Scope = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    ScopeId = table.Column<Guid>(type: "char(36)", nullable: true),
                    TenantId = table.Column<Guid>(type: "char(36)", nullable: true),
                    SectionKey = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    Json = table.Column<string>(type: "longtext", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenancy_SettingValues", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "tenancy_Tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    Code = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    Isolation = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    DatabaseProvider = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true),
                    ConnectionSecretRef = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true),
                    Plan = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
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
                    table.PrimaryKey("PK_tenancy_Tenants", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "tenancy_TenantDomains",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    TenantId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Host = table.Column<string>(type: "varchar(253)", maxLength: 253, nullable: false),
                    IsPrimary = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenancy_TenantDomains", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tenancy_TenantDomains_tenancy_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenancy_Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "tenancy_TenantFeatures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    TenantId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Feature = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    Enabled = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenancy_TenantFeatures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tenancy_TenantFeatures_tenancy_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenancy_Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_tenancy_OutboxMessages_ProcessedAt_OccurredAt",
                table: "tenancy_OutboxMessages",
                columns: new[] { "ProcessedAt", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_tenancy_SettingValues_SectionKey_Scope_ScopeId_TenantId",
                table: "tenancy_SettingValues",
                columns: new[] { "SectionKey", "Scope", "ScopeId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_tenancy_TenantDomains_Host",
                table: "tenancy_TenantDomains",
                column: "Host",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenancy_TenantDomains_TenantId",
                table: "tenancy_TenantDomains",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_tenancy_TenantFeatures_TenantId_Feature",
                table: "tenancy_TenantFeatures",
                columns: new[] { "TenantId", "Feature" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenancy_Tenants_Code",
                table: "tenancy_Tenants",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenancy_Tenants_IsDeleted",
                table: "tenancy_Tenants",
                column: "IsDeleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tenancy_OutboxMessages");

            migrationBuilder.DropTable(
                name: "tenancy_SettingValues");

            migrationBuilder.DropTable(
                name: "tenancy_TenantDomains");

            migrationBuilder.DropTable(
                name: "tenancy_TenantFeatures");

            migrationBuilder.DropTable(
                name: "tenancy_Tenants");
        }
    }
}
