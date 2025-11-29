using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConsignmentGenie.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OnboardingDismissedField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "OnboardingDismissed",
                table: "Organizations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Organizations",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "OnboardingDismissed", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 29, 0, 44, 15, 813, DateTimeKind.Utc).AddTicks(9594), false, new DateTime(2025, 11, 29, 0, 44, 15, 813, DateTimeKind.Utc).AddTicks(9595) });

            migrationBuilder.UpdateData(
                table: "Providers",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 29, 0, 44, 16, 401, DateTimeKind.Utc).AddTicks(6583), new DateTime(2025, 11, 29, 0, 44, 16, 401, DateTimeKind.Utc).AddTicks(6585) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 29, 0, 44, 16, 401, DateTimeKind.Utc).AddTicks(6265), "$2a$11$eyMk393KPx9GY283.ONJ.u.mXdbdy0wff7.5IYHeplKcTIivsi62W", new DateTime(2025, 11, 29, 0, 44, 16, 401, DateTimeKind.Utc).AddTicks(6306) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 29, 0, 44, 16, 401, DateTimeKind.Utc).AddTicks(6334), "$2a$11$eyMk393KPx9GY283.ONJ.u.mXdbdy0wff7.5IYHeplKcTIivsi62W", new DateTime(2025, 11, 29, 0, 44, 16, 401, DateTimeKind.Utc).AddTicks(6334) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 29, 0, 44, 16, 401, DateTimeKind.Utc).AddTicks(6349), "$2a$11$eyMk393KPx9GY283.ONJ.u.mXdbdy0wff7.5IYHeplKcTIivsi62W", new DateTime(2025, 11, 29, 0, 44, 16, 401, DateTimeKind.Utc).AddTicks(6350) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 29, 0, 44, 16, 401, DateTimeKind.Utc).AddTicks(6367), "$2a$11$eyMk393KPx9GY283.ONJ.u.mXdbdy0wff7.5IYHeplKcTIivsi62W", new DateTime(2025, 11, 29, 0, 44, 16, 401, DateTimeKind.Utc).AddTicks(6367) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OnboardingDismissed",
                table: "Organizations");

            migrationBuilder.UpdateData(
                table: "Organizations",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 25, 19, 56, 48, 723, DateTimeKind.Utc).AddTicks(3693), new DateTime(2025, 11, 25, 19, 56, 48, 723, DateTimeKind.Utc).AddTicks(3695) });

            migrationBuilder.UpdateData(
                table: "Providers",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 25, 19, 56, 49, 323, DateTimeKind.Utc).AddTicks(5038), new DateTime(2025, 11, 25, 19, 56, 49, 323, DateTimeKind.Utc).AddTicks(5039) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 25, 19, 56, 49, 323, DateTimeKind.Utc).AddTicks(4449), "$2a$11$ziLGJ44gFXAO3yDwZ9FlsOtBrmoWyR27Fn6MBV48jaTBqXYv6oB5G", new DateTime(2025, 11, 25, 19, 56, 49, 323, DateTimeKind.Utc).AddTicks(4494) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 25, 19, 56, 49, 323, DateTimeKind.Utc).AddTicks(4531), "$2a$11$ziLGJ44gFXAO3yDwZ9FlsOtBrmoWyR27Fn6MBV48jaTBqXYv6oB5G", new DateTime(2025, 11, 25, 19, 56, 49, 323, DateTimeKind.Utc).AddTicks(4532) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 25, 19, 56, 49, 323, DateTimeKind.Utc).AddTicks(4707), "$2a$11$ziLGJ44gFXAO3yDwZ9FlsOtBrmoWyR27Fn6MBV48jaTBqXYv6oB5G", new DateTime(2025, 11, 25, 19, 56, 49, 323, DateTimeKind.Utc).AddTicks(4712) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 25, 19, 56, 49, 323, DateTimeKind.Utc).AddTicks(4757), "$2a$11$ziLGJ44gFXAO3yDwZ9FlsOtBrmoWyR27Fn6MBV48jaTBqXYv6oB5G", new DateTime(2025, 11, 25, 19, 56, 49, 323, DateTimeKind.Utc).AddTicks(4758) });
        }
    }
}
