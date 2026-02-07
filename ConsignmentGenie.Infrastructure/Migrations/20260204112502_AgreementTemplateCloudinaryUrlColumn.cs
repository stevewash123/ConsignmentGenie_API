using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConsignmentGenie.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgreementTemplateCloudinaryUrlColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AgreementTemplateId",
                table: "Organizations");

            migrationBuilder.AddColumn<string>(
                name: "AgreementTemplateCloudinaryUrl",
                table: "Organizations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

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
                columns: new[] { "AgreementTemplateCloudinaryUrl", "CreatedAt", "UpdatedAt" },
                values: new object[] { null, new DateTime(2026, 2, 4, 11, 24, 59, 649, DateTimeKind.Utc).AddTicks(9194), new DateTime(2026, 2, 4, 11, 24, 59, 649, DateTimeKind.Utc).AddTicks(9195) });

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AgreementTemplateCloudinaryUrl",
                table: "Organizations");

            migrationBuilder.AddColumn<Guid>(
                name: "AgreementTemplateId",
                table: "Organizations",
                type: "uuid",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Consignors",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 4, 4, 35, 34, 516, DateTimeKind.Utc).AddTicks(938), new DateTime(2026, 2, 4, 4, 35, 34, 516, DateTimeKind.Utc).AddTicks(938) });

            migrationBuilder.UpdateData(
                table: "Organizations",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "AgreementTemplateId", "CreatedAt", "UpdatedAt" },
                values: new object[] { null, new DateTime(2026, 2, 4, 4, 35, 33, 951, DateTimeKind.Utc).AddTicks(2480), new DateTime(2026, 2, 4, 4, 35, 33, 951, DateTimeKind.Utc).AddTicks(2482) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 4, 4, 35, 34, 516, DateTimeKind.Utc).AddTicks(651), "$2a$11$YRCBRRH6MSKhysX8HOp7NudvMU25HfLCP4u8O1RWr2/ikcjXUEmJu", new DateTime(2026, 2, 4, 4, 35, 34, 516, DateTimeKind.Utc).AddTicks(696) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 4, 4, 35, 34, 516, DateTimeKind.Utc).AddTicks(724), "$2a$11$YRCBRRH6MSKhysX8HOp7NudvMU25HfLCP4u8O1RWr2/ikcjXUEmJu", new DateTime(2026, 2, 4, 4, 35, 34, 516, DateTimeKind.Utc).AddTicks(725) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 4, 4, 35, 34, 516, DateTimeKind.Utc).AddTicks(740), "$2a$11$YRCBRRH6MSKhysX8HOp7NudvMU25HfLCP4u8O1RWr2/ikcjXUEmJu", new DateTime(2026, 2, 4, 4, 35, 34, 516, DateTimeKind.Utc).AddTicks(740) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 4, 4, 35, 34, 516, DateTimeKind.Utc).AddTicks(758), "$2a$11$YRCBRRH6MSKhysX8HOp7NudvMU25HfLCP4u8O1RWr2/ikcjXUEmJu", new DateTime(2026, 2, 4, 4, 35, 34, 516, DateTimeKind.Utc).AddTicks(759) });
        }
    }
}
