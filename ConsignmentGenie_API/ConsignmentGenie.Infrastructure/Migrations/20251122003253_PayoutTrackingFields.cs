using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConsignmentGenie.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PayoutTrackingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PayoutMethod",
                table: "Transactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayoutNotes",
                table: "Transactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ProviderPaidOut",
                table: "Transactions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProviderPaidOutDate",
                table: "Transactions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Organizations",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 22, 0, 32, 51, 574, DateTimeKind.Utc).AddTicks(9625), new DateTime(2025, 11, 22, 0, 32, 51, 574, DateTimeKind.Utc).AddTicks(9626) });

            migrationBuilder.UpdateData(
                table: "Providers",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 22, 0, 32, 51, 784, DateTimeKind.Utc).AddTicks(6749), new DateTime(2025, 11, 22, 0, 32, 51, 784, DateTimeKind.Utc).AddTicks(6749) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 22, 0, 32, 51, 784, DateTimeKind.Utc).AddTicks(6433), "$2a$11$KHGfgJJZI.b0Q/rbR/iCZuOlxEo.25tPPE1Z8AvdrOQ/QtPoxKqYW", new DateTime(2025, 11, 22, 0, 32, 51, 784, DateTimeKind.Utc).AddTicks(6445) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 22, 0, 32, 51, 784, DateTimeKind.Utc).AddTicks(6475), "$2a$11$KHGfgJJZI.b0Q/rbR/iCZuOlxEo.25tPPE1Z8AvdrOQ/QtPoxKqYW", new DateTime(2025, 11, 22, 0, 32, 51, 784, DateTimeKind.Utc).AddTicks(6476) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 22, 0, 32, 51, 784, DateTimeKind.Utc).AddTicks(6490), "$2a$11$KHGfgJJZI.b0Q/rbR/iCZuOlxEo.25tPPE1Z8AvdrOQ/QtPoxKqYW", new DateTime(2025, 11, 22, 0, 32, 51, 784, DateTimeKind.Utc).AddTicks(6491) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 22, 0, 32, 51, 784, DateTimeKind.Utc).AddTicks(6506), "$2a$11$KHGfgJJZI.b0Q/rbR/iCZuOlxEo.25tPPE1Z8AvdrOQ/QtPoxKqYW", new DateTime(2025, 11, 22, 0, 32, 51, 784, DateTimeKind.Utc).AddTicks(6506) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PayoutMethod",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "PayoutNotes",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ProviderPaidOut",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ProviderPaidOutDate",
                table: "Transactions");

            migrationBuilder.UpdateData(
                table: "Organizations",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 21, 21, 30, 45, 153, DateTimeKind.Utc).AddTicks(4273), new DateTime(2025, 11, 21, 21, 30, 45, 153, DateTimeKind.Utc).AddTicks(4274) });

            migrationBuilder.UpdateData(
                table: "Providers",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 21, 21, 30, 45, 353, DateTimeKind.Utc).AddTicks(3901), new DateTime(2025, 11, 21, 21, 30, 45, 353, DateTimeKind.Utc).AddTicks(3901) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 21, 21, 30, 45, 353, DateTimeKind.Utc).AddTicks(3762), "$2a$11$2S3AR0kDNXjMw5PJP8z0KOP317r2ygroQVF6Tqiek/L3WZbcfTtsy", new DateTime(2025, 11, 21, 21, 30, 45, 353, DateTimeKind.Utc).AddTicks(3769) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 21, 21, 30, 45, 353, DateTimeKind.Utc).AddTicks(3778), "$2a$11$2S3AR0kDNXjMw5PJP8z0KOP317r2ygroQVF6Tqiek/L3WZbcfTtsy", new DateTime(2025, 11, 21, 21, 30, 45, 353, DateTimeKind.Utc).AddTicks(3778) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 21, 21, 30, 45, 353, DateTimeKind.Utc).AddTicks(3781), "$2a$11$2S3AR0kDNXjMw5PJP8z0KOP317r2ygroQVF6Tqiek/L3WZbcfTtsy", new DateTime(2025, 11, 21, 21, 30, 45, 353, DateTimeKind.Utc).AddTicks(3782) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 21, 21, 30, 45, 353, DateTimeKind.Utc).AddTicks(3785), "$2a$11$2S3AR0kDNXjMw5PJP8z0KOP317r2ygroQVF6Tqiek/L3WZbcfTtsy", new DateTime(2025, 11, 21, 21, 30, 45, 353, DateTimeKind.Utc).AddTicks(3785) });
        }
    }
}
