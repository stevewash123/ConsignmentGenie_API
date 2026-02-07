using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConsignmentGenie.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBrandToPendingImportItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Brand",
                table: "PendingImportItems",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Consignors",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 5, 19, 50, 29, 940, DateTimeKind.Utc).AddTicks(2019), new DateTime(2026, 2, 5, 19, 50, 29, 940, DateTimeKind.Utc).AddTicks(2020) });

            migrationBuilder.UpdateData(
                table: "Organizations",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 5, 19, 50, 29, 393, DateTimeKind.Utc).AddTicks(6155), new DateTime(2026, 2, 5, 19, 50, 29, 393, DateTimeKind.Utc).AddTicks(6156) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 5, 19, 50, 29, 940, DateTimeKind.Utc).AddTicks(1732), "$2a$11$DdAPNYL7lOTNa8o6fgVehu1iaPosf0apTtK8XGIF7mKuqDbMzk322", new DateTime(2026, 2, 5, 19, 50, 29, 940, DateTimeKind.Utc).AddTicks(1770) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 5, 19, 50, 29, 940, DateTimeKind.Utc).AddTicks(1802), "$2a$11$DdAPNYL7lOTNa8o6fgVehu1iaPosf0apTtK8XGIF7mKuqDbMzk322", new DateTime(2026, 2, 5, 19, 50, 29, 940, DateTimeKind.Utc).AddTicks(1803) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 5, 19, 50, 29, 940, DateTimeKind.Utc).AddTicks(1817), "$2a$11$DdAPNYL7lOTNa8o6fgVehu1iaPosf0apTtK8XGIF7mKuqDbMzk322", new DateTime(2026, 2, 5, 19, 50, 29, 940, DateTimeKind.Utc).AddTicks(1818) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 5, 19, 50, 29, 940, DateTimeKind.Utc).AddTicks(1832), "$2a$11$DdAPNYL7lOTNa8o6fgVehu1iaPosf0apTtK8XGIF7mKuqDbMzk322", new DateTime(2026, 2, 5, 19, 50, 29, 940, DateTimeKind.Utc).AddTicks(1833) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Brand",
                table: "PendingImportItems");

            migrationBuilder.UpdateData(
                table: "Consignors",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 5, 12, 41, 50, 476, DateTimeKind.Utc).AddTicks(8784), new DateTime(2026, 2, 5, 12, 41, 50, 476, DateTimeKind.Utc).AddTicks(8786) });

            migrationBuilder.UpdateData(
                table: "Organizations",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 5, 12, 41, 49, 856, DateTimeKind.Utc).AddTicks(3358), new DateTime(2026, 2, 5, 12, 41, 49, 856, DateTimeKind.Utc).AddTicks(3359) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 5, 12, 41, 50, 476, DateTimeKind.Utc).AddTicks(8493), "$2a$11$PCj54nebs2YSS4tciP5W7eCJwo./9ZlaPNQwMmYryrqSqU4auyo/C", new DateTime(2026, 2, 5, 12, 41, 50, 476, DateTimeKind.Utc).AddTicks(8530) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 5, 12, 41, 50, 476, DateTimeKind.Utc).AddTicks(8561), "$2a$11$PCj54nebs2YSS4tciP5W7eCJwo./9ZlaPNQwMmYryrqSqU4auyo/C", new DateTime(2026, 2, 5, 12, 41, 50, 476, DateTimeKind.Utc).AddTicks(8561) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 5, 12, 41, 50, 476, DateTimeKind.Utc).AddTicks(8580), "$2a$11$PCj54nebs2YSS4tciP5W7eCJwo./9ZlaPNQwMmYryrqSqU4auyo/C", new DateTime(2026, 2, 5, 12, 41, 50, 476, DateTimeKind.Utc).AddTicks(8581) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 5, 12, 41, 50, 476, DateTimeKind.Utc).AddTicks(8598), "$2a$11$PCj54nebs2YSS4tciP5W7eCJwo./9ZlaPNQwMmYryrqSqU4auyo/C", new DateTime(2026, 2, 5, 12, 41, 50, 476, DateTimeKind.Utc).AddTicks(8599) });
        }
    }
}
