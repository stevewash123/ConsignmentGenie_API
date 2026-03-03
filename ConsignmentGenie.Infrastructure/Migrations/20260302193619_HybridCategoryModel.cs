using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConsignmentGenie.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class HybridCategoryModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ConsignorId",
                table: "ItemCategories",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Source",
                table: "ItemCategories",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "ItemCategories",
                type: "integer",
                nullable: false,
                defaultValue: 0);

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

            migrationBuilder.CreateIndex(
                name: "IX_ItemCategories_ConsignorId",
                table: "ItemCategories",
                column: "ConsignorId");

            // Create filtered unique index for active categories only (Amendment 1 requirement)
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX IX_Categories_OrgName_Active
                ON ""ItemCategories""(""OrganizationId"", ""Name"")
                WHERE ""Status"" = 0;  -- 0 = Active
            ");

            migrationBuilder.AddForeignKey(
                name: "FK_ItemCategories_Consignors_ConsignorId",
                table: "ItemCategories",
                column: "ConsignorId",
                principalTable: "Consignors",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ItemCategories_Consignors_ConsignorId",
                table: "ItemCategories");

            // Drop the filtered unique index
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS IX_Categories_OrgName_Active;");

            migrationBuilder.DropIndex(
                name: "IX_ItemCategories_ConsignorId",
                table: "ItemCategories");

            migrationBuilder.DropColumn(
                name: "ConsignorId",
                table: "ItemCategories");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "ItemCategories");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ItemCategories");

            migrationBuilder.UpdateData(
                table: "Consignors",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 27, 22, 22, 41, 817, DateTimeKind.Utc).AddTicks(541), new DateTime(2026, 2, 27, 22, 22, 41, 817, DateTimeKind.Utc).AddTicks(542) });

            migrationBuilder.UpdateData(
                table: "Organizations",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 27, 22, 22, 41, 259, DateTimeKind.Utc).AddTicks(5588), new DateTime(2026, 2, 27, 22, 22, 41, 259, DateTimeKind.Utc).AddTicks(5589) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 27, 22, 22, 41, 817, DateTimeKind.Utc).AddTicks(289), "$2a$11$l3kC36joLNBC9qKcbDshYOKT4h9C7Q49mLd8GzjkjBpWLcR.8PQtK", new DateTime(2026, 2, 27, 22, 22, 41, 817, DateTimeKind.Utc).AddTicks(292) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 27, 22, 22, 41, 817, DateTimeKind.Utc).AddTicks(318), "$2a$11$l3kC36joLNBC9qKcbDshYOKT4h9C7Q49mLd8GzjkjBpWLcR.8PQtK", new DateTime(2026, 2, 27, 22, 22, 41, 817, DateTimeKind.Utc).AddTicks(353) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 27, 22, 22, 41, 817, DateTimeKind.Utc).AddTicks(373), "$2a$11$l3kC36joLNBC9qKcbDshYOKT4h9C7Q49mLd8GzjkjBpWLcR.8PQtK", new DateTime(2026, 2, 27, 22, 22, 41, 817, DateTimeKind.Utc).AddTicks(373) });
        }
    }
}
