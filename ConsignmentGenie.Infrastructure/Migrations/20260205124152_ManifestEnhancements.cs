using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConsignmentGenie.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ManifestEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImagePublicId",
                table: "PendingImportItems",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "PendingImportItems",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinimumPrice",
                table: "PendingImportItems",
                type: "numeric(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinimumTotal",
                table: "DropoffRequests",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PhotosPurgedAt",
                table: "DropoffRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedAt",
                table: "DropoffRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "DropoffRequests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReopenedAt",
                table: "DropoffRequests",
                type: "timestamp with time zone",
                nullable: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagePublicId",
                table: "PendingImportItems");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "PendingImportItems");

            migrationBuilder.DropColumn(
                name: "MinimumPrice",
                table: "PendingImportItems");

            migrationBuilder.DropColumn(
                name: "MinimumTotal",
                table: "DropoffRequests");

            migrationBuilder.DropColumn(
                name: "PhotosPurgedAt",
                table: "DropoffRequests");

            migrationBuilder.DropColumn(
                name: "RejectedAt",
                table: "DropoffRequests");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "DropoffRequests");

            migrationBuilder.DropColumn(
                name: "ReopenedAt",
                table: "DropoffRequests");

            migrationBuilder.UpdateData(
                table: "Consignors",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 4, 11, 25, 0, 213, DateTimeKind.Utc).AddTicks(4993), new DateTime(2026, 2, 4, 11, 25, 0, 213, DateTimeKind.Utc).AddTicks(4993) });

            migrationBuilder.UpdateData(
                table: "Organizations",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 4, 11, 24, 59, 649, DateTimeKind.Utc).AddTicks(9194), new DateTime(2026, 2, 4, 11, 24, 59, 649, DateTimeKind.Utc).AddTicks(9195) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 4, 11, 25, 0, 213, DateTimeKind.Utc).AddTicks(4705), "$2a$11$g1vqQ0qa/moV9N3nDRAEYefrOjAyH3qInztaPkVfRXSFlpq.heq7a", new DateTime(2026, 2, 4, 11, 25, 0, 213, DateTimeKind.Utc).AddTicks(4751) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 4, 11, 25, 0, 213, DateTimeKind.Utc).AddTicks(4778), "$2a$11$g1vqQ0qa/moV9N3nDRAEYefrOjAyH3qInztaPkVfRXSFlpq.heq7a", new DateTime(2026, 2, 4, 11, 25, 0, 213, DateTimeKind.Utc).AddTicks(4779) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 4, 11, 25, 0, 213, DateTimeKind.Utc).AddTicks(4797), "$2a$11$g1vqQ0qa/moV9N3nDRAEYefrOjAyH3qInztaPkVfRXSFlpq.heq7a", new DateTime(2026, 2, 4, 11, 25, 0, 213, DateTimeKind.Utc).AddTicks(4797) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 4, 11, 25, 0, 213, DateTimeKind.Utc).AddTicks(4814), "$2a$11$g1vqQ0qa/moV9N3nDRAEYefrOjAyH3qInztaPkVfRXSFlpq.heq7a", new DateTime(2026, 2, 4, 11, 25, 0, 213, DateTimeKind.Utc).AddTicks(4814) });
        }
    }
}
