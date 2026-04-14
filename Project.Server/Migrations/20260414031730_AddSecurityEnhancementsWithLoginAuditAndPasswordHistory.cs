using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Project.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddSecurityEnhancementsWithLoginAuditAndPasswordHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Modules",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Image = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Path = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Modules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Operations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Guid = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Policy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Path = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ModuleId = table.Column<long>(type: "bigint", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    ControllerName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ActionName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    HttpMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RouteTemplate = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OperationKey = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Operations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Operations_Modules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "Modules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RolId = table.Column<long>(type: "bigint", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    IdentificationDocument = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    RecoveryToken = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DateToken = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Reset = table.Column<bool>(type: "bit", nullable: true),
                    Number = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    MustChangePassword = table.Column<bool>(type: "bit", nullable: false),
                    LastPasswordChange = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailedLoginAttempts = table.Column<int>(type: "int", nullable: false),
                    LockoutEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    State = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Roles_RolId",
                        column: x => x.RolId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RolOperations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RolId = table.Column<long>(type: "bigint", nullable: false),
                    OperationId = table.Column<long>(type: "bigint", nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolOperations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolOperations_Operations_OperationId",
                        column: x => x.OperationId,
                        principalTable: "Operations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolOperations_Roles_RolId",
                        column: x => x.RolId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LoginAudits",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LoginSuccessful = table.Column<bool>(type: "bit", nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LoginDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginAudits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoginAudits_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PasswordHistories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangedBy = table.Column<long>(type: "bigint", nullable: false),
                    ForceChange = table.Column<bool>(type: "bit", nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasswordHistories_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "Modules",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "Description", "Image", "Name", "Order", "Path", "State", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { 1L, new DateTime(2025, 2, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), 1L, "Modulo de Pagos", "mail-open", "Pagos", 0, "Payments", 1, null, null },
                    { 2L, new DateTime(2025, 2, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), 1L, "Modulo de Cobros Automaticos", "apps", "Cobros Automaticos", 0, "AutomaticPayments", 1, null, null },
                    { 3L, new DateTime(2025, 2, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), 1L, "Modulo de Cursos", "cash", "Cursos", 0, "Courses", 1, null, null },
                    { 4L, new DateTime(2025, 2, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), 1L, "Modulo Estado de Cuenta", "cash", "Estado de Cuenta", 0, "AccountStatement", 1, null, null }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "Description", "Name", "State", "UpdatedAt", "UpdatedBy" },
                values: new object[] { 1L, new DateTime(2025, 2, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), 1L, "Super Administrator", "SA", 1, null, null });

            migrationBuilder.InsertData(
                table: "Operations",
                columns: new[] { "Id", "ActionName", "ControllerName", "CreatedAt", "CreatedBy", "Description", "Guid", "HttpMethod", "Icon", "IsVisible", "ModuleId", "Name", "OperationKey", "Path", "Policy", "RouteTemplate", "State", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { 1L, "", "", new DateTime(2025, 2, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), 1L, "Consultar Pagos", "6451551a-b05c-455b-b1b9-97616e1c8892", "", "list", true, 1L, "Listar Pagos", "", "Payments", "Payments.List", "", 1, null, null },
                    { 2L, "", "", new DateTime(2025, 2, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), 1L, "Creacion de pagos", "2e26b4ca-bd5d-4c4f-a027-ba09f5bd448f", "", "cash", false, 1L, "Crear Pago", "", "Payments/Create", "Payments.Create", "", 1, null, null },
                    { 3L, "", "", new DateTime(2025, 2, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), 1L, "Listar Cobros Automaticos", "3fd82baf-a73d-4809-8508-60dbec6119b0", "", "list", true, 2L, "Listar Cobros Automaticos", "", "AutomaticPayments", "AutomaticPayments.List", "", 1, null, null },
                    { 4L, "", "", new DateTime(2025, 2, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), 1L, "Crear Cobros Automaticos", "3f3f1b3b-6b0d-4eb4-8eee-cee97771f293", "", "list", true, 2L, "Crear Cobros Automaticos", "", "AutomaticPayments", "AutomaticPayments.Create", "", 1, null, null },
                    { 5L, "", "", new DateTime(2025, 2, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), 1L, "Listar Cursos", "def16355-a62d-4e96-8549-66d6cf66352b", "", "list", true, 3L, "Listar Cursos", "", "Courses", "Courses.List", "", 1, null, null },
                    { 6L, "", "", new DateTime(2025, 2, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), 1L, "Crear Cursos", "0487750b-e5d1-434a-892c-98c8df359ce1", "", "list", true, 3L, "Crear Cursos", "", "Courses/Create", "Courses.Create", "", 1, null, null },
                    { 7L, "", "", new DateTime(2025, 2, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), 1L, "Actualizar Cursos", "89775173-5789-4fbd-984a-14215b4ec5d4", "", "list", false, 3L, "Actualizar Cursos", "", "Courses/Update", "Courses.Update", "", 1, null, null },
                    { 8L, "", "", new DateTime(2025, 2, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), 1L, "Eliminar Cursos", "578a0423-c554-4cac-b5db-ef66f3dec7be", "", "list", false, 3L, "Eliminar Cursos", "", "Courses/Delete", "AutomaticPayments.Delete", "", 1, null, null },
                    { 9L, "", "", new DateTime(2025, 2, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), 1L, "Estado de Cuenta Estudiante", "aecb492c-69ad-480d-9646-d589b0baf93a", "", "list", true, 4L, "Estado de Cuenta Estudiante", "", "AccountStatement/Student", "AccountStatement.Student", "", 1, null, null },
                    { 10L, "", "", new DateTime(2025, 2, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), 1L, "Estado de Cuenta Global", "07e595dd-7330-4948-b4ab-853144ca8cbb", "", "list", true, 4L, "Estado de Cuenta Global", "", "AccountStatement/Admin", "AccountStatement.Admin", "", 1, null, null }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DateToken", "Email", "FailedLoginAttempts", "IdentificationDocument", "LastPasswordChange", "LockoutEnd", "MustChangePassword", "Name", "Number", "Password", "RecoveryToken", "Reset", "RolId", "State", "UpdatedAt", "UpdatedBy", "UserName" },
                values: new object[] { 1L, new DateTime(2025, 2, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), 1L, null, "pruebas.test29111999@gmail.com", 0, "2987967910101", null, null, false, "Super Administrador", "51995142", "$2a$12$86Ty8oUVWKPbU8JqCII9VO.FgM1C10dweQ4xKhM4jj1LWL9jwNu7.", "", false, 1L, 1, null, null, "SADMIN" });

            migrationBuilder.InsertData(
                table: "RolOperations",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "OperationId", "RolId", "State", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { 1L, new DateTime(2025, 2, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), 1L, 1L, 1L, 1, null, null },
                    { 2L, new DateTime(2025, 2, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), 1L, 2L, 1L, 1, null, null },
                    { 3L, new DateTime(2025, 2, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), 1L, 3L, 1L, 1, null, null },
                    { 4L, new DateTime(2025, 2, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), 1L, 4L, 1L, 1, null, null },
                    { 5L, new DateTime(2025, 2, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), 1L, 5L, 1L, 1, null, null },
                    { 6L, new DateTime(2025, 2, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), 1L, 6L, 1L, 1, null, null },
                    { 7L, new DateTime(2025, 2, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), 1L, 7L, 1L, 1, null, null },
                    { 8L, new DateTime(2025, 2, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), 1L, 8L, 1L, 1, null, null },
                    { 9L, new DateTime(2025, 2, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), 1L, 9L, 1L, 1, null, null },
                    { 10L, new DateTime(2025, 2, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), 1L, 10L, 1L, 1, null, null },
                    { 11L, new DateTime(2025, 2, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), 1L, 2L, 2L, 1, null, null },
                    { 12L, new DateTime(2025, 2, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), 1L, 5L, 2L, 1, null, null },
                    { 13L, new DateTime(2025, 2, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), 1L, 9L, 2L, 1, null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_LoginAudits_UserId",
                table: "LoginAudits",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Operations_ModuleId",
                table: "Operations",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordHistories_UserId",
                table: "PasswordHistories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RolOperations_OperationId",
                table: "RolOperations",
                column: "OperationId");

            migrationBuilder.CreateIndex(
                name: "IX_RolOperations_RolId",
                table: "RolOperations",
                column: "RolId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_RolId",
                table: "Users",
                column: "RolId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoginAudits");

            migrationBuilder.DropTable(
                name: "PasswordHistories");

            migrationBuilder.DropTable(
                name: "RolOperations");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Operations");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Modules");
        }
    }
}
