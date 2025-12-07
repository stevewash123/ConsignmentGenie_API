using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConsignmentGenie.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRequireAgreementOnFileToOrganizations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RequireAgreementOnFile",
                table: "Organizations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Consignors",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 12, 7, 12, 29, 3, 681, DateTimeKind.Utc).AddTicks(1133), new DateTime(2025, 12, 7, 12, 29, 3, 681, DateTimeKind.Utc).AddTicks(1133) });

            migrationBuilder.UpdateData(
                table: "Organizations",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "RequireAgreementOnFile", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 12, 7, 12, 29, 3, 130, DateTimeKind.Utc).AddTicks(7050), false, new DateTime(2025, 12, 7, 12, 29, 3, 130, DateTimeKind.Utc).AddTicks(7052) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 12, 7, 12, 29, 3, 681, DateTimeKind.Utc).AddTicks(872), "$2a$11$Sznr2tiV3nopxHzITb7EmuFhB6BSL2.dcAPfcDSQmoNcoOj1e7U6u", new DateTime(2025, 12, 7, 12, 29, 3, 681, DateTimeKind.Utc).AddTicks(905) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 12, 7, 12, 29, 3, 681, DateTimeKind.Utc).AddTicks(931), "$2a$11$Sznr2tiV3nopxHzITb7EmuFhB6BSL2.dcAPfcDSQmoNcoOj1e7U6u", new DateTime(2025, 12, 7, 12, 29, 3, 681, DateTimeKind.Utc).AddTicks(931) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 12, 7, 12, 29, 3, 681, DateTimeKind.Utc).AddTicks(947), "$2a$11$Sznr2tiV3nopxHzITb7EmuFhB6BSL2.dcAPfcDSQmoNcoOj1e7U6u", new DateTime(2025, 12, 7, 12, 29, 3, 681, DateTimeKind.Utc).AddTicks(948) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 12, 7, 12, 29, 3, 681, DateTimeKind.Utc).AddTicks(963), "$2a$11$Sznr2tiV3nopxHzITb7EmuFhB6BSL2.dcAPfcDSQmoNcoOj1e7U6u", new DateTime(2025, 12, 7, 12, 29, 3, 681, DateTimeKind.Utc).AddTicks(964) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequireAgreementOnFile",
                table: "Organizations");

            migrationBuilder.UpdateData(
                table: "Consignors",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 12, 7, 12, 16, 49, 874, DateTimeKind.Utc).AddTicks(6172), new DateTime(2025, 12, 7, 12, 16, 49, 874, DateTimeKind.Utc).AddTicks(6173) });

            migrationBuilder.UpdateData(
                table: "Organizations",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 12, 7, 12, 16, 49, 310, DateTimeKind.Utc).AddTicks(1393), new DateTime(2025, 12, 7, 12, 16, 49, 310, DateTimeKind.Utc).AddTicks(1395) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 12, 7, 12, 16, 49, 874, DateTimeKind.Utc).AddTicks(5862), "$2a$11$xl1PtT23ydwlpDyP2taDiuwhR8QxSpYXD2QOzD15caHyrj7aMpJKe", new DateTime(2025, 12, 7, 12, 16, 49, 874, DateTimeKind.Utc).AddTicks(5906) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 12, 7, 12, 16, 49, 874, DateTimeKind.Utc).AddTicks(5936), "$2a$11$xl1PtT23ydwlpDyP2taDiuwhR8QxSpYXD2QOzD15caHyrj7aMpJKe", new DateTime(2025, 12, 7, 12, 16, 49, 874, DateTimeKind.Utc).AddTicks(5937) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 12, 7, 12, 16, 49, 874, DateTimeKind.Utc).AddTicks(5956), "$2a$11$xl1PtT23ydwlpDyP2taDiuwhR8QxSpYXD2QOzD15caHyrj7aMpJKe", new DateTime(2025, 12, 7, 12, 16, 49, 874, DateTimeKind.Utc).AddTicks(5956) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 12, 7, 12, 16, 49, 874, DateTimeKind.Utc).AddTicks(5974), "$2a$11$xl1PtT23ydwlpDyP2taDiuwhR8QxSpYXD2QOzD15caHyrj7aMpJKe", new DateTime(2025, 12, 7, 12, 16, 49, 874, DateTimeKind.Utc).AddTicks(5974) });
        }
    }
}
