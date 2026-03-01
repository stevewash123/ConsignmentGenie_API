using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConsignmentGenie.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CustomerAccountsAndSimplifiedReservations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reservations_OrganizationId_CustomerPhone",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "CustomerEmail",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "CustomerName",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "CustomerPhone",
                table: "Reservations");

            migrationBuilder.AddColumn<Guid>(
                name: "CustomerId",
                table: "Reservations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "VerificationMethod",
                table: "Reservations",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    GoogleId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AppleId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FirstReservationAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NotifyReservationReminders = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Customers_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Consignors",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 27, 22, 22, 41, 817, DateTimeKind.Utc).AddTicks(541), new DateTime(2026, 2, 27, 22, 22, 41, 817, DateTimeKind.Utc).AddTicks(542) });

            migrationBuilder.UpdateData(
                table: "Organizations",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 27, 22, 22, 41, 259, DateTimeKind.Utc).AddTicks(5588), new DateTime(2026, 2, 27, 22, 22, 41, 259, DateTimeKind.Utc).AddTicks(5589) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 27, 22, 22, 41, 817, DateTimeKind.Utc).AddTicks(289), "$2a$11$l3kC36joLNBC9qKcbDshYOKT4h9C7Q49mLd8GzjkjBpWLcR.8PQtK", new DateTime(2026, 2, 27, 22, 22, 41, 817, DateTimeKind.Utc).AddTicks(292) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 27, 22, 22, 41, 817, DateTimeKind.Utc).AddTicks(318), "$2a$11$l3kC36joLNBC9qKcbDshYOKT4h9C7Q49mLd8GzjkjBpWLcR.8PQtK", new DateTime(2026, 2, 27, 22, 22, 41, 817, DateTimeKind.Utc).AddTicks(353) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 27, 22, 22, 41, 817, DateTimeKind.Utc).AddTicks(373), "$2a$11$l3kC36joLNBC9qKcbDshYOKT4h9C7Q49mLd8GzjkjBpWLcR.8PQtK", new DateTime(2026, 2, 27, 22, 22, 41, 817, DateTimeKind.Utc).AddTicks(373) });

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_CustomerId",
                table: "Reservations",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_AppleId",
                table: "Customers",
                column: "AppleId",
                unique: true,
                filter: "\"AppleId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CreatedAt",
                table: "Customers",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_GoogleId",
                table: "Customers",
                column: "GoogleId",
                unique: true,
                filter: "\"GoogleId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_OrganizationId",
                table: "Customers",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_OrganizationId_Email",
                table: "Customers",
                columns: new[] { "OrganizationId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_PhoneNumber",
                table: "Customers",
                column: "PhoneNumber");

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_Customers_CustomerId",
                table: "Reservations",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_Customers_CustomerId",
                table: "Reservations");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_CustomerId",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "VerificationMethod",
                table: "Reservations");

            migrationBuilder.AddColumn<string>(
                name: "CustomerEmail",
                table: "Reservations",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerName",
                table: "Reservations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CustomerPhone",
                table: "Reservations",
                type: "character varying(15)",
                maxLength: 15,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Consignors",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 27, 4, 0, 44, 692, DateTimeKind.Utc).AddTicks(740), new DateTime(2026, 2, 27, 4, 0, 44, 692, DateTimeKind.Utc).AddTicks(741) });

            migrationBuilder.UpdateData(
                table: "Organizations",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 27, 4, 0, 44, 125, DateTimeKind.Utc).AddTicks(4707), new DateTime(2026, 2, 27, 4, 0, 44, 125, DateTimeKind.Utc).AddTicks(4708) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 27, 4, 0, 44, 692, DateTimeKind.Utc).AddTicks(470), "$2a$11$ErLitSseups.NSrGXGWHpeOjXlgid7chvEuC90vywQ2oIqiUUn.Kq", new DateTime(2026, 2, 27, 4, 0, 44, 692, DateTimeKind.Utc).AddTicks(472) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 27, 4, 0, 44, 692, DateTimeKind.Utc).AddTicks(501), "$2a$11$ErLitSseups.NSrGXGWHpeOjXlgid7chvEuC90vywQ2oIqiUUn.Kq", new DateTime(2026, 2, 27, 4, 0, 44, 692, DateTimeKind.Utc).AddTicks(536) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 27, 4, 0, 44, 692, DateTimeKind.Utc).AddTicks(555), "$2a$11$ErLitSseups.NSrGXGWHpeOjXlgid7chvEuC90vywQ2oIqiUUn.Kq", new DateTime(2026, 2, 27, 4, 0, 44, 692, DateTimeKind.Utc).AddTicks(555) });

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_OrganizationId_CustomerPhone",
                table: "Reservations",
                columns: new[] { "OrganizationId", "CustomerPhone" });
        }
    }
}
