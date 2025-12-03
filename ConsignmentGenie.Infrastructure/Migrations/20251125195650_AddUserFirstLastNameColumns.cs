using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConsignmentGenie.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserFirstLastNameColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "Users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "Users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsUsed",
                table: "ProviderInvitations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UsedAt",
                table: "ProviderInvitations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Organizations",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 25, 19, 56, 48, 723, DateTimeKind.Utc).AddTicks(3693), new DateTime(2025, 11, 25, 19, 56, 48, 723, DateTimeKind.Utc).AddTicks(3695) });

            migrationBuilder.UpdateData(
                table: "Consignors",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 25, 19, 56, 49, 323, DateTimeKind.Utc).AddTicks(5038), new DateTime(2025, 11, 25, 19, 56, 49, 323, DateTimeKind.Utc).AddTicks(5039) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "FirstName", "LastName", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 25, 19, 56, 49, 323, DateTimeKind.Utc).AddTicks(4449), null, null, "$2a$11$ziLGJ44gFXAO3yDwZ9FlsOtBrmoWyR27Fn6MBV48jaTBqXYv6oB5G", new DateTime(2025, 11, 25, 19, 56, 49, 323, DateTimeKind.Utc).AddTicks(4494) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "FirstName", "LastName", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 25, 19, 56, 49, 323, DateTimeKind.Utc).AddTicks(4531), null, null, "$2a$11$ziLGJ44gFXAO3yDwZ9FlsOtBrmoWyR27Fn6MBV48jaTBqXYv6oB5G", new DateTime(2025, 11, 25, 19, 56, 49, 323, DateTimeKind.Utc).AddTicks(4532) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "FirstName", "LastName", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 25, 19, 56, 49, 323, DateTimeKind.Utc).AddTicks(4707), null, null, "$2a$11$ziLGJ44gFXAO3yDwZ9FlsOtBrmoWyR27Fn6MBV48jaTBqXYv6oB5G", new DateTime(2025, 11, 25, 19, 56, 49, 323, DateTimeKind.Utc).AddTicks(4712) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "FirstName", "LastName", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 25, 19, 56, 49, 323, DateTimeKind.Utc).AddTicks(4757), null, null, "$2a$11$ziLGJ44gFXAO3yDwZ9FlsOtBrmoWyR27Fn6MBV48jaTBqXYv6oB5G", new DateTime(2025, 11, 25, 19, 56, 49, 323, DateTimeKind.Utc).AddTicks(4758) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsUsed",
                table: "ProviderInvitations");

            migrationBuilder.DropColumn(
                name: "UsedAt",
                table: "ProviderInvitations");

            migrationBuilder.UpdateData(
                table: "Organizations",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 25, 18, 10, 43, 185, DateTimeKind.Utc).AddTicks(4164), new DateTime(2025, 11, 25, 18, 10, 43, 185, DateTimeKind.Utc).AddTicks(4165) });

            migrationBuilder.UpdateData(
                table: "Consignors",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 25, 18, 10, 43, 734, DateTimeKind.Utc).AddTicks(9358), new DateTime(2025, 11, 25, 18, 10, 43, 734, DateTimeKind.Utc).AddTicks(9358) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 25, 18, 10, 43, 734, DateTimeKind.Utc).AddTicks(9129), "$2a$11$G2cWPDE69h1YXfcJYCFZGeATWoiF1uED58WKAab0xs2h9An1DtqDm", new DateTime(2025, 11, 25, 18, 10, 43, 734, DateTimeKind.Utc).AddTicks(9141) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 25, 18, 10, 43, 734, DateTimeKind.Utc).AddTicks(9166), "$2a$11$G2cWPDE69h1YXfcJYCFZGeATWoiF1uED58WKAab0xs2h9An1DtqDm", new DateTime(2025, 11, 25, 18, 10, 43, 734, DateTimeKind.Utc).AddTicks(9167) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 25, 18, 10, 43, 734, DateTimeKind.Utc).AddTicks(9182), "$2a$11$G2cWPDE69h1YXfcJYCFZGeATWoiF1uED58WKAab0xs2h9An1DtqDm", new DateTime(2025, 11, 25, 18, 10, 43, 734, DateTimeKind.Utc).AddTicks(9183) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 25, 18, 10, 43, 734, DateTimeKind.Utc).AddTicks(9199), "$2a$11$G2cWPDE69h1YXfcJYCFZGeATWoiF1uED58WKAab0xs2h9An1DtqDm", new DateTime(2025, 11, 25, 18, 10, 43, 734, DateTimeKind.Utc).AddTicks(9200) });
        }
    }
}
