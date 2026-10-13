using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project.Migrations.MySql.Notifications
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
                name: "notifications_Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    TenantId = table.Column<Guid>(type: "char(36)", nullable: false),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Template = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    Title = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false),
                    Body = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: false),
                    Link = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    ReadAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications_Notifications", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "notifications_OutboxMessages",
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
                    table.PrimaryKey("PK_notifications_OutboxMessages", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "notifications_TemplateOverrides",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    TenantId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Key = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    Language = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false),
                    Subject = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false),
                    Body = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: false),
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
                    table.PrimaryKey("PK_notifications_TemplateOverrides", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_Notifications_TenantId",
                table: "notifications_Notifications",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_Notifications_TenantId_UserId_CreatedAt",
                table: "notifications_Notifications",
                columns: new[] { "TenantId", "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_notifications_Notifications_TenantId_UserId_ReadAt",
                table: "notifications_Notifications",
                columns: new[] { "TenantId", "UserId", "ReadAt" });

            migrationBuilder.CreateIndex(
                name: "IX_notifications_OutboxMessages_ProcessedAt_OccurredAt",
                table: "notifications_OutboxMessages",
                columns: new[] { "ProcessedAt", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_notifications_TemplateOverrides_IsDeleted",
                table: "notifications_TemplateOverrides",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_TemplateOverrides_TenantId",
                table: "notifications_TemplateOverrides",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_TemplateOverrides_TenantId_Key_Language",
                table: "notifications_TemplateOverrides",
                columns: new[] { "TenantId", "Key", "Language" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notifications_Notifications");

            migrationBuilder.DropTable(
                name: "notifications_OutboxMessages");

            migrationBuilder.DropTable(
                name: "notifications_TemplateOverrides");
        }
    }
}
