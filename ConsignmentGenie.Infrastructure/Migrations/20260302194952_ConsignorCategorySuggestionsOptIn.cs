using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConsignmentGenie.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConsignorCategorySuggestionsOptIn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowConsignorCategorySuggestions",
                table: "Organizations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Consignors",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 2, 19, 49, 50, 498, DateTimeKind.Utc).AddTicks(5150), new DateTime(2026, 3, 2, 19, 49, 50, 498, DateTimeKind.Utc).AddTicks(5150) });

            migrationBuilder.UpdateData(
                table: "Organizations",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "AllowConsignorCategorySuggestions", "CreatedAt", "UpdatedAt" },
                values: new object[] { false, new DateTime(2026, 3, 2, 19, 49, 49, 923, DateTimeKind.Utc).AddTicks(2416), new DateTime(2026, 3, 2, 19, 49, 49, 923, DateTimeKind.Utc).AddTicks(2417) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 2, 19, 49, 50, 498, DateTimeKind.Utc).AddTicks(4879), "$2a$11$KtGQKTeJ47VZN7PwVdog0u6YqhJfpa5H/r3QEfDl53FbQdZBfEax6", new DateTime(2026, 3, 2, 19, 49, 50, 498, DateTimeKind.Utc).AddTicks(4880) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 2, 19, 49, 50, 498, DateTimeKind.Utc).AddTicks(4912), "$2a$11$KtGQKTeJ47VZN7PwVdog0u6YqhJfpa5H/r3QEfDl53FbQdZBfEax6", new DateTime(2026, 3, 2, 19, 49, 50, 498, DateTimeKind.Utc).AddTicks(4947) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 2, 19, 49, 50, 498, DateTimeKind.Utc).AddTicks(4969), "$2a$11$KtGQKTeJ47VZN7PwVdog0u6YqhJfpa5H/r3QEfDl53FbQdZBfEax6", new DateTime(2026, 3, 2, 19, 49, 50, 498, DateTimeKind.Utc).AddTicks(4969) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowConsignorCategorySuggestions",
                table: "Organizations");

            migrationBuilder.UpdateData(
                table: "Consignors",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 2, 19, 36, 17, 884, DateTimeKind.Utc).AddTicks(5354), new DateTime(2026, 3, 2, 19, 36, 17, 884, DateTimeKind.Utc).AddTicks(5355) });

            migrationBuilder.UpdateData(
                table: "Organizations",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 2, 19, 36, 17, 315, DateTimeKind.Utc).AddTicks(5671), new DateTime(2026, 3, 2, 19, 36, 17, 315, DateTimeKind.Utc).AddTicks(5672) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 2, 19, 36, 17, 884, DateTimeKind.Utc).AddTicks(5104), "$2a$11$Y1tHSIDm4mpna7xWnpt2LuCDSiP0wEMQ3rlHGOjqXe1Er.xypxR5W", new DateTime(2026, 3, 2, 19, 36, 17, 884, DateTimeKind.Utc).AddTicks(5106) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 2, 19, 36, 17, 884, DateTimeKind.Utc).AddTicks(5131), "$2a$11$Y1tHSIDm4mpna7xWnpt2LuCDSiP0wEMQ3rlHGOjqXe1Er.xypxR5W", new DateTime(2026, 3, 2, 19, 36, 17, 884, DateTimeKind.Utc).AddTicks(5166) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 2, 19, 36, 17, 884, DateTimeKind.Utc).AddTicks(5183), "$2a$11$Y1tHSIDm4mpna7xWnpt2LuCDSiP0wEMQ3rlHGOjqXe1Er.xypxR5W", new DateTime(2026, 3, 2, 19, 36, 17, 884, DateTimeKind.Utc).AddTicks(5184) });
        }
    }
}
