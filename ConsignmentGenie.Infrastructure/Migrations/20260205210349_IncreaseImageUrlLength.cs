using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConsignmentGenie.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IncreaseImageUrlLength : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Consignors",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 5, 21, 3, 47, 898, DateTimeKind.Utc).AddTicks(8232), new DateTime(2026, 2, 5, 21, 3, 47, 898, DateTimeKind.Utc).AddTicks(8233) });

            migrationBuilder.UpdateData(
                table: "Organizations",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 5, 21, 3, 47, 334, DateTimeKind.Utc).AddTicks(5542), new DateTime(2026, 2, 5, 21, 3, 47, 334, DateTimeKind.Utc).AddTicks(5543) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 5, 21, 3, 47, 898, DateTimeKind.Utc).AddTicks(7955), "$2a$11$S6QX48tK7gxmHCuN7.vX6OCtKBWmvCqW3m6qYqQw4joULlxJk1PGC", new DateTime(2026, 2, 5, 21, 3, 47, 898, DateTimeKind.Utc).AddTicks(7994) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 5, 21, 3, 47, 898, DateTimeKind.Utc).AddTicks(8025), "$2a$11$S6QX48tK7gxmHCuN7.vX6OCtKBWmvCqW3m6qYqQw4joULlxJk1PGC", new DateTime(2026, 2, 5, 21, 3, 47, 898, DateTimeKind.Utc).AddTicks(8026) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 5, 21, 3, 47, 898, DateTimeKind.Utc).AddTicks(8043), "$2a$11$S6QX48tK7gxmHCuN7.vX6OCtKBWmvCqW3m6qYqQw4joULlxJk1PGC", new DateTime(2026, 2, 5, 21, 3, 47, 898, DateTimeKind.Utc).AddTicks(8044) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 5, 21, 3, 47, 898, DateTimeKind.Utc).AddTicks(8059), "$2a$11$S6QX48tK7gxmHCuN7.vX6OCtKBWmvCqW3m6qYqQw4joULlxJk1PGC", new DateTime(2026, 2, 5, 21, 3, 47, 898, DateTimeKind.Utc).AddTicks(8059) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
    }
}
