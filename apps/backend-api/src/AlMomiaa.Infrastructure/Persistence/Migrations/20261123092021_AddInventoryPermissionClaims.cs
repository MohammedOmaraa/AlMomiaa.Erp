using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AlMomiaa.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryPermissionClaims : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "id",
                table: "RolePermissions",
                columns: new[] { "PermissionClaim", "RoleId" },
                values: new object[,]
                {
                    { "inventory:adjust", 1 },
                    { "inventory:transfer", 1 },
                    { "inventory:view", 1 },
                    { "inventory:view", 2 },
                    { "inventory:adjust", 6 },
                    { "inventory:transfer", 6 },
                    { "inventory:view", 6 },
                    { "inventory:transfer", 7 },
                    { "inventory:view", 7 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "id",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionClaim", "RoleId" },
                keyValues: new object[] { "inventory:adjust", 1 });

            migrationBuilder.DeleteData(
                schema: "id",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionClaim", "RoleId" },
                keyValues: new object[] { "inventory:transfer", 1 });

            migrationBuilder.DeleteData(
                schema: "id",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionClaim", "RoleId" },
                keyValues: new object[] { "inventory:view", 1 });

            migrationBuilder.DeleteData(
                schema: "id",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionClaim", "RoleId" },
                keyValues: new object[] { "inventory:view", 2 });

            migrationBuilder.DeleteData(
                schema: "id",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionClaim", "RoleId" },
                keyValues: new object[] { "inventory:adjust", 6 });

            migrationBuilder.DeleteData(
                schema: "id",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionClaim", "RoleId" },
                keyValues: new object[] { "inventory:transfer", 6 });

            migrationBuilder.DeleteData(
                schema: "id",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionClaim", "RoleId" },
                keyValues: new object[] { "inventory:view", 6 });

            migrationBuilder.DeleteData(
                schema: "id",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionClaim", "RoleId" },
                keyValues: new object[] { "inventory:transfer", 7 });

            migrationBuilder.DeleteData(
                schema: "id",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionClaim", "RoleId" },
                keyValues: new object[] { "inventory:view", 7 });
        }
    }
}
