using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConsignmentGenie.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ProviderToConsignorRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Consignor",
                table: "PaymentGatewayConnections",
                newName: "Provider");

            migrationBuilder.RenameIndex(
                name: "IX_PaymentGatewayConnections_OrganizationId_Consignor_IsActive",
                table: "PaymentGatewayConnections",
                newName: "IX_PaymentGatewayConnections_OrganizationId_Provider_IsActive");

            migrationBuilder.UpdateData(
                table: "Consignors",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 12, 3, 1, 37, 37, 121, DateTimeKind.Utc).AddTicks(1437), new DateTime(2025, 12, 3, 1, 37, 37, 121, DateTimeKind.Utc).AddTicks(1437) });

            migrationBuilder.UpdateData(
                table: "Organizations",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 12, 3, 1, 37, 36, 553, DateTimeKind.Utc).AddTicks(402), new DateTime(2025, 12, 3, 1, 37, 36, 553, DateTimeKind.Utc).AddTicks(403) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 12, 3, 1, 37, 37, 121, DateTimeKind.Utc).AddTicks(894), "$2a$11$IQE3n1sGoM6L7RxoR2097esYCv0oWI4HMw.WqpU9dU0OUjqBVLfJ.", new DateTime(2025, 12, 3, 1, 37, 37, 121, DateTimeKind.Utc).AddTicks(933) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 12, 3, 1, 37, 37, 121, DateTimeKind.Utc).AddTicks(962), "$2a$11$IQE3n1sGoM6L7RxoR2097esYCv0oWI4HMw.WqpU9dU0OUjqBVLfJ.", new DateTime(2025, 12, 3, 1, 37, 37, 121, DateTimeKind.Utc).AddTicks(962) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 12, 3, 1, 37, 37, 121, DateTimeKind.Utc).AddTicks(1169), "$2a$11$IQE3n1sGoM6L7RxoR2097esYCv0oWI4HMw.WqpU9dU0OUjqBVLfJ.", new DateTime(2025, 12, 3, 1, 37, 37, 121, DateTimeKind.Utc).AddTicks(1169) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 12, 3, 1, 37, 37, 121, DateTimeKind.Utc).AddTicks(1187), "$2a$11$IQE3n1sGoM6L7RxoR2097esYCv0oWI4HMw.WqpU9dU0OUjqBVLfJ.", new DateTime(2025, 12, 3, 1, 37, 37, 121, DateTimeKind.Utc).AddTicks(1187) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Provider",
                table: "PaymentGatewayConnections",
                newName: "Consignor");

            migrationBuilder.RenameIndex(
                name: "IX_PaymentGatewayConnections_OrganizationId_Provider_IsActive",
                table: "PaymentGatewayConnections",
                newName: "IX_PaymentGatewayConnections_OrganizationId_Consignor_IsActive");

            migrationBuilder.UpdateData(
                table: "Consignors",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 30, 13, 48, 38, 415, DateTimeKind.Utc).AddTicks(6287), new DateTime(2025, 11, 30, 13, 48, 38, 415, DateTimeKind.Utc).AddTicks(6288) });

            migrationBuilder.UpdateData(
                table: "Organizations",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 30, 13, 48, 37, 852, DateTimeKind.Utc).AddTicks(1702), new DateTime(2025, 11, 30, 13, 48, 37, 852, DateTimeKind.Utc).AddTicks(1704) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 30, 13, 48, 38, 415, DateTimeKind.Utc).AddTicks(5996), "$2a$11$WLKp9sKXgZaWBhKTjCtVJetdu44MVnJ/pedeCoEO/8xtMznP4j9z.", new DateTime(2025, 11, 30, 13, 48, 38, 415, DateTimeKind.Utc).AddTicks(6039) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 30, 13, 48, 38, 415, DateTimeKind.Utc).AddTicks(6068), "$2a$11$WLKp9sKXgZaWBhKTjCtVJetdu44MVnJ/pedeCoEO/8xtMznP4j9z.", new DateTime(2025, 11, 30, 13, 48, 38, 415, DateTimeKind.Utc).AddTicks(6069) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 30, 13, 48, 38, 415, DateTimeKind.Utc).AddTicks(6086), "$2a$11$WLKp9sKXgZaWBhKTjCtVJetdu44MVnJ/pedeCoEO/8xtMznP4j9z.", new DateTime(2025, 11, 30, 13, 48, 38, 415, DateTimeKind.Utc).AddTicks(6087) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 30, 13, 48, 38, 415, DateTimeKind.Utc).AddTicks(6104), "$2a$11$WLKp9sKXgZaWBhKTjCtVJetdu44MVnJ/pedeCoEO/8xtMznP4j9z.", new DateTime(2025, 11, 30, 13, 48, 38, 415, DateTimeKind.Utc).AddTicks(6104) });
        }
    }
}
