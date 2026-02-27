using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConsignmentGenie.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StripeConnectIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StripeAccessToken",
                table: "Organizations",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeAccountId",
                table: "Organizations",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StripeConnectedAt",
                table: "Organizations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "StripePayoutsEnabled",
                table: "Organizations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "StripePublishableKey",
                table: "Organizations",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeRefreshToken",
                table: "Organizations",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Consignors",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 23, 16, 50, 26, 913, DateTimeKind.Utc).AddTicks(1513), new DateTime(2026, 2, 23, 16, 50, 26, 913, DateTimeKind.Utc).AddTicks(1514) });

            migrationBuilder.UpdateData(
                table: "Organizations",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "StripeAccessToken", "StripeAccountId", "StripeConnectedAt", "StripePayoutsEnabled", "StripePublishableKey", "StripeRefreshToken", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 23, 16, 50, 26, 360, DateTimeKind.Utc).AddTicks(8331), null, null, null, false, null, null, new DateTime(2026, 2, 23, 16, 50, 26, 360, DateTimeKind.Utc).AddTicks(8332) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 23, 16, 50, 26, 913, DateTimeKind.Utc).AddTicks(1234), "$2a$11$.v1oX1.fnbSMHYYBA0m0oucB2FAoC.jUmjR1cY1Rc1kcTIVZWLyP2", new DateTime(2026, 2, 23, 16, 50, 26, 913, DateTimeKind.Utc).AddTicks(1274) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 23, 16, 50, 26, 913, DateTimeKind.Utc).AddTicks(1304), "$2a$11$.v1oX1.fnbSMHYYBA0m0oucB2FAoC.jUmjR1cY1Rc1kcTIVZWLyP2", new DateTime(2026, 2, 23, 16, 50, 26, 913, DateTimeKind.Utc).AddTicks(1305) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 23, 16, 50, 26, 913, DateTimeKind.Utc).AddTicks(1320), "$2a$11$.v1oX1.fnbSMHYYBA0m0oucB2FAoC.jUmjR1cY1Rc1kcTIVZWLyP2", new DateTime(2026, 2, 23, 16, 50, 26, 913, DateTimeKind.Utc).AddTicks(1321) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 23, 16, 50, 26, 913, DateTimeKind.Utc).AddTicks(1336), "$2a$11$.v1oX1.fnbSMHYYBA0m0oucB2FAoC.jUmjR1cY1Rc1kcTIVZWLyP2", new DateTime(2026, 2, 23, 16, 50, 26, 913, DateTimeKind.Utc).AddTicks(1336) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StripeAccessToken",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "StripeAccountId",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "StripeConnectedAt",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "StripePayoutsEnabled",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "StripePublishableKey",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "StripeRefreshToken",
                table: "Organizations");

            migrationBuilder.UpdateData(
                table: "Consignors",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 21, 20, 28, 8, 374, DateTimeKind.Utc).AddTicks(752), new DateTime(2026, 2, 21, 20, 28, 8, 374, DateTimeKind.Utc).AddTicks(753) });

            migrationBuilder.UpdateData(
                table: "Organizations",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 21, 20, 28, 7, 828, DateTimeKind.Utc).AddTicks(5734), new DateTime(2026, 2, 21, 20, 28, 7, 828, DateTimeKind.Utc).AddTicks(5734) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 21, 20, 28, 8, 374, DateTimeKind.Utc).AddTicks(433), "$2a$11$RQyy6RL9J489/Isgx7elFu.hHhUy7ExCfNQFJHyofvK/GbiucOVw6", new DateTime(2026, 2, 21, 20, 28, 8, 374, DateTimeKind.Utc).AddTicks(465) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 21, 20, 28, 8, 374, DateTimeKind.Utc).AddTicks(511), "$2a$11$RQyy6RL9J489/Isgx7elFu.hHhUy7ExCfNQFJHyofvK/GbiucOVw6", new DateTime(2026, 2, 21, 20, 28, 8, 374, DateTimeKind.Utc).AddTicks(512) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 21, 20, 28, 8, 374, DateTimeKind.Utc).AddTicks(542), "$2a$11$RQyy6RL9J489/Isgx7elFu.hHhUy7ExCfNQFJHyofvK/GbiucOVw6", new DateTime(2026, 2, 21, 20, 28, 8, 374, DateTimeKind.Utc).AddTicks(542) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 21, 20, 28, 8, 374, DateTimeKind.Utc).AddTicks(558), "$2a$11$RQyy6RL9J489/Isgx7elFu.hHhUy7ExCfNQFJHyofvK/GbiucOVw6", new DateTime(2026, 2, 21, 20, 28, 8, 374, DateTimeKind.Utc).AddTicks(559) });
        }
    }
}
