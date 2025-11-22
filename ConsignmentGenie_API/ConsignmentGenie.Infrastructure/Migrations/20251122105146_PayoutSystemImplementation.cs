using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConsignmentGenie.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PayoutSystemImplementation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ItemsIncluded",
                table: "Payouts");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "Payouts");

            migrationBuilder.DropColumn(
                name: "QuickBooksSyncError",
                table: "Payouts");

            migrationBuilder.DropColumn(
                name: "QuickBooksSyncFailed",
                table: "Payouts");

            migrationBuilder.DropColumn(
                name: "SyncedAt",
                table: "Payouts");

            migrationBuilder.DropColumn(
                name: "TotalAmount",
                table: "Payouts");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Payouts");

            migrationBuilder.AddColumn<Guid>(
                name: "PayoutId",
                table: "Transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayoutStatus",
                table: "Transactions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "PaymentMethod",
                table: "Payouts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Amount",
                table: "Payouts",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "Payouts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentReference",
                table: "Payouts",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PayoutDate",
                table: "Payouts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "PayoutNumber",
                table: "Payouts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TransactionCount",
                table: "Payouts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Organizations",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 22, 10, 51, 45, 56, DateTimeKind.Utc).AddTicks(4696), new DateTime(2025, 11, 22, 10, 51, 45, 56, DateTimeKind.Utc).AddTicks(4698) });

            migrationBuilder.UpdateData(
                table: "Providers",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 22, 10, 51, 45, 262, DateTimeKind.Utc).AddTicks(8498), new DateTime(2025, 11, 22, 10, 51, 45, 262, DateTimeKind.Utc).AddTicks(8499) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 22, 10, 51, 45, 262, DateTimeKind.Utc).AddTicks(8190), "$2a$11$DtUnvMdlCUrZseSPU/h2N.GV2O15D.DWcf1UziXd6R/5yVS96.D9K", new DateTime(2025, 11, 22, 10, 51, 45, 262, DateTimeKind.Utc).AddTicks(8215) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 22, 10, 51, 45, 262, DateTimeKind.Utc).AddTicks(8243), "$2a$11$DtUnvMdlCUrZseSPU/h2N.GV2O15D.DWcf1UziXd6R/5yVS96.D9K", new DateTime(2025, 11, 22, 10, 51, 45, 262, DateTimeKind.Utc).AddTicks(8244) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 22, 10, 51, 45, 262, DateTimeKind.Utc).AddTicks(8259), "$2a$11$DtUnvMdlCUrZseSPU/h2N.GV2O15D.DWcf1UziXd6R/5yVS96.D9K", new DateTime(2025, 11, 22, 10, 51, 45, 262, DateTimeKind.Utc).AddTicks(8260) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 22, 10, 51, 45, 262, DateTimeKind.Utc).AddTicks(8276), "$2a$11$DtUnvMdlCUrZseSPU/h2N.GV2O15D.DWcf1UziXd6R/5yVS96.D9K", new DateTime(2025, 11, 22, 10, 51, 45, 262, DateTimeKind.Utc).AddTicks(8276) });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_PayoutId",
                table: "Transactions",
                column: "PayoutId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Payouts_PayoutId",
                table: "Transactions",
                column: "PayoutId",
                principalTable: "Payouts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Payouts_PayoutId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_PayoutId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "PayoutId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "PayoutStatus",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Amount",
                table: "Payouts");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Payouts");

            migrationBuilder.DropColumn(
                name: "PaymentReference",
                table: "Payouts");

            migrationBuilder.DropColumn(
                name: "PayoutDate",
                table: "Payouts");

            migrationBuilder.DropColumn(
                name: "PayoutNumber",
                table: "Payouts");

            migrationBuilder.DropColumn(
                name: "TransactionCount",
                table: "Payouts");

            migrationBuilder.AlterColumn<string>(
                name: "PaymentMethod",
                table: "Payouts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<string>(
                name: "ItemsIncluded",
                table: "Payouts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "Payouts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QuickBooksSyncError",
                table: "Payouts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "QuickBooksSyncFailed",
                table: "Payouts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SyncedAt",
                table: "Payouts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalAmount",
                table: "Payouts",
                type: "numeric(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Payouts",
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
    }
}
