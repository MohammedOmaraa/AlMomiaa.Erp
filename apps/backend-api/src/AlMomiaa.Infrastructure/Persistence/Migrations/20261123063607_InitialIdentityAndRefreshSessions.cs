using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AlMomiaa.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialIdentityAndRefreshSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "id");

            migrationBuilder.CreateTable(
                name: "Roles",
                schema: "id",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "id",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FullNameAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    FullNameEn = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RepType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DefaultWarehouseId = table.Column<int>(type: "int", nullable: true),
                    SecurityStamp = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                schema: "id",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    PermissionClaim = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => new { x.RoleId, x.PermissionClaim });
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "id",
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokenSessions",
                schema: "id",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FamilyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    IssuedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReplacedBySessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RevocationReason = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokenSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokenSessions_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "id",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                schema: "id",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "id",
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "id",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserWarehouses",
                schema: "id",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserWarehouses", x => new { x.UserId, x.WarehouseId });
                    table.ForeignKey(
                        name: "FK_UserWarehouses_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "id",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "id",
                table: "Roles",
                columns: new[] { "Id", "NameAr", "NameEn", "RoleCode" },
                values: new object[,]
                {
                    { 1, "مدير النظام", "Super Admin", "SUPER_ADMIN" },
                    { 2, "المدير التنفيذي", "Executive Manager", "EXEC_MGR" },
                    { 3, "مدير المبيعات", "Sales Manager", "SALES_MGR" },
                    { 4, "مندوب مبيعات", "Sales Representative", "SALES_REP" },
                    { 5, "محاسب", "Accountant", "ACCOUNTANT" },
                    { 6, "أمين مستودع", "Warehouse Keeper", "WH_KEEPER" },
                    { 7, "مدير المشتريات", "Purchasing Manager", "PURCH_MGR" }
                });

            migrationBuilder.InsertData(
                schema: "id",
                table: "RolePermissions",
                columns: new[] { "PermissionClaim", "RoleId" },
                values: new object[,]
                {
                    { "audit:view", 1 },
                    { "catalog:manage", 1 },
                    { "catalog:view", 1 },
                    { "collections:create", 1 },
                    { "customers:manage", 1 },
                    { "customers:view", 1 },
                    { "dashboards:executive", 1 },
                    { "dashboards:finance", 1 },
                    { "dashboards:inventory", 1 },
                    { "dashboards:profit", 1 },
                    { "dashboards:sales", 1 },
                    { "delivery:dispatch", 1 },
                    { "delivery:update_pod", 1 },
                    { "finance:close_period", 1 },
                    { "finance:confirm_treasury", 1 },
                    { "finance:verify_bank", 1 },
                    { "invoices:upload_copy", 1 },
                    { "invoices:view", 1 },
                    { "orders:approve", 1 },
                    { "orders:coapprove_credit", 1 },
                    { "orders:create", 1 },
                    { "orders:edit_submitted", 1 },
                    { "orders:override_price", 1 },
                    { "permissions:manage", 1 },
                    { "procurement:manage", 1 },
                    { "procurement:receive", 1 },
                    { "sales:returns", 1 },
                    { "settings:manage", 1 },
                    { "users:manage", 1 },
                    { "warehouse:ship", 1 },
                    { "audit:view", 2 },
                    { "catalog:view", 2 },
                    { "customers:view", 2 },
                    { "dashboards:executive", 2 },
                    { "dashboards:finance", 2 },
                    { "dashboards:inventory", 2 },
                    { "dashboards:profit", 2 },
                    { "dashboards:sales", 2 },
                    { "finance:close_period", 2 },
                    { "invoices:view", 2 },
                    { "orders:approve", 2 },
                    { "orders:coapprove_credit", 2 },
                    { "orders:override_price", 2 },
                    { "procurement:manage", 2 },
                    { "sales:returns", 2 },
                    { "settings:manage", 2 },
                    { "catalog:manage", 3 },
                    { "catalog:view", 3 },
                    { "customers:manage", 3 },
                    { "customers:view", 3 },
                    { "dashboards:finance", 3 },
                    { "dashboards:inventory", 3 },
                    { "dashboards:sales", 3 },
                    { "invoices:upload_copy", 3 },
                    { "invoices:view", 3 },
                    { "orders:approve", 3 },
                    { "orders:create", 3 },
                    { "orders:edit_submitted", 3 },
                    { "orders:override_price", 3 },
                    { "sales:returns", 3 },
                    { "users:manage", 3 },
                    { "catalog:view", 4 },
                    { "collections:create", 4 },
                    { "customers:view", 4 },
                    { "dashboards:finance", 4 },
                    { "dashboards:inventory", 4 },
                    { "dashboards:sales", 4 },
                    { "delivery:update_pod", 4 },
                    { "gps:stream", 4 },
                    { "invoices:upload_copy", 4 },
                    { "invoices:view", 4 },
                    { "orders:create", 4 },
                    { "catalog:view", 5 },
                    { "customers:manage", 5 },
                    { "customers:view", 5 },
                    { "dashboards:finance", 5 },
                    { "dashboards:inventory", 5 },
                    { "dashboards:profit", 5 },
                    { "finance:close_period", 5 },
                    { "finance:confirm_treasury", 5 },
                    { "finance:verify_bank", 5 },
                    { "invoices:upload_copy", 5 },
                    { "invoices:view", 5 },
                    { "orders:coapprove_credit", 5 },
                    { "sales:returns", 5 },
                    { "catalog:view", 6 },
                    { "customers:view", 6 },
                    { "dashboards:inventory", 6 },
                    { "delivery:dispatch", 6 },
                    { "delivery:update_pod", 6 },
                    { "invoices:upload_copy", 6 },
                    { "invoices:view", 6 },
                    { "procurement:receive", 6 },
                    { "sales:returns", 6 },
                    { "warehouse:ship", 6 },
                    { "catalog:manage", 7 },
                    { "catalog:view", 7 },
                    { "dashboards:inventory", 7 },
                    { "procurement:manage", 7 },
                    { "procurement:receive", 7 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokenSessions_FamilyId",
                schema: "id",
                table: "RefreshTokenSessions",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokenSessions_User_Active",
                schema: "id",
                table: "RefreshTokenSessions",
                columns: new[] { "UserId", "RevokedAtUtc", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_RefreshTokenSessions_TokenHash",
                schema: "id",
                table: "RefreshTokenSessions",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Roles_RoleCode",
                schema: "id",
                table: "Roles",
                column: "RoleCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                schema: "id",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "UQ_Users_EmployeeCode",
                schema: "id",
                table: "Users",
                column: "EmployeeCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Users_Phone",
                schema: "id",
                table: "Users",
                column: "Phone",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RefreshTokenSessions",
                schema: "id");

            migrationBuilder.DropTable(
                name: "RolePermissions",
                schema: "id");

            migrationBuilder.DropTable(
                name: "UserRoles",
                schema: "id");

            migrationBuilder.DropTable(
                name: "UserWarehouses",
                schema: "id");

            migrationBuilder.DropTable(
                name: "Roles",
                schema: "id");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "id");
        }
    }
}
