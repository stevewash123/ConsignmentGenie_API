using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConsignmentGenie.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ClerkRoleSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClerkPin",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "HiredDate",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLoginAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcessedByName",
                table: "Transactions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProcessedByUserId",
                table: "Transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ClerkPermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShowProviderNames = table.Column<bool>(type: "boolean", nullable: false),
                    ShowItemCost = table.Column<bool>(type: "boolean", nullable: false),
                    AllowReturns = table.Column<bool>(type: "boolean", nullable: false),
                    MaxReturnAmountWithoutPin = table.Column<decimal>(type: "numeric", nullable: false),
                    AllowDiscounts = table.Column<bool>(type: "boolean", nullable: false),
                    MaxDiscountPercentWithoutPin = table.Column<int>(type: "integer", nullable: false),
                    AllowVoid = table.Column<bool>(type: "boolean", nullable: false),
                    AllowDrawerOpen = table.Column<bool>(type: "boolean", nullable: false),
                    AllowEndOfDayCount = table.Column<bool>(type: "boolean", nullable: false),
                    AllowPriceOverride = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClerkPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClerkPermissions_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Organizations",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 29, 22, 19, 58, 821, DateTimeKind.Utc).AddTicks(8413), new DateTime(2025, 11, 29, 22, 19, 58, 821, DateTimeKind.Utc).AddTicks(8414) });

            migrationBuilder.UpdateData(
                table: "Providers",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 29, 22, 19, 59, 351, DateTimeKind.Utc).AddTicks(8245), new DateTime(2025, 11, 29, 22, 19, 59, 351, DateTimeKind.Utc).AddTicks(8246) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "ClerkPin", "CreatedAt", "HiredDate", "IsActive", "LastLoginAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { null, new DateTime(2025, 11, 29, 22, 19, 59, 351, DateTimeKind.Utc).AddTicks(7969), null, true, null, "$2a$11$nv1e.TRcEATUJ/txPQYRs.LfxE18Zgywc71LC5lCvKaVGdSsWkyUm", new DateTime(2025, 11, 29, 22, 19, 59, 351, DateTimeKind.Utc).AddTicks(8006) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "ClerkPin", "CreatedAt", "HiredDate", "IsActive", "LastLoginAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { null, new DateTime(2025, 11, 29, 22, 19, 59, 351, DateTimeKind.Utc).AddTicks(8034), null, true, null, "$2a$11$nv1e.TRcEATUJ/txPQYRs.LfxE18Zgywc71LC5lCvKaVGdSsWkyUm", new DateTime(2025, 11, 29, 22, 19, 59, 351, DateTimeKind.Utc).AddTicks(8035) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "ClerkPin", "CreatedAt", "HiredDate", "IsActive", "LastLoginAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { null, new DateTime(2025, 11, 29, 22, 19, 59, 351, DateTimeKind.Utc).AddTicks(8051), null, true, null, "$2a$11$nv1e.TRcEATUJ/txPQYRs.LfxE18Zgywc71LC5lCvKaVGdSsWkyUm", new DateTime(2025, 11, 29, 22, 19, 59, 351, DateTimeKind.Utc).AddTicks(8051) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "ClerkPin", "CreatedAt", "HiredDate", "IsActive", "LastLoginAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { null, new DateTime(2025, 11, 29, 22, 19, 59, 351, DateTimeKind.Utc).AddTicks(8068), null, true, null, "$2a$11$nv1e.TRcEATUJ/txPQYRs.LfxE18Zgywc71LC5lCvKaVGdSsWkyUm", new DateTime(2025, 11, 29, 22, 19, 59, 351, DateTimeKind.Utc).AddTicks(8068) });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ProcessedByUserId",
                table: "Transactions",
                column: "ProcessedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ClerkPermissions_OrganizationId",
                table: "ClerkPermissions",
                column: "OrganizationId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Users_ProcessedByUserId",
                table: "Transactions",
                column: "ProcessedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Users_ProcessedByUserId",
                table: "Transactions");

            migrationBuilder.DropTable(
                name: "ClerkPermissions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_ProcessedByUserId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ClerkPin",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "HiredDate",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastLoginAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ProcessedByName",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ProcessedByUserId",
                table: "Transactions");

            migrationBuilder.UpdateData(
                table: "Organizations",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 29, 0, 44, 15, 813, DateTimeKind.Utc).AddTicks(9594), new DateTime(2025, 11, 29, 0, 44, 15, 813, DateTimeKind.Utc).AddTicks(9595) });

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
    }
}
