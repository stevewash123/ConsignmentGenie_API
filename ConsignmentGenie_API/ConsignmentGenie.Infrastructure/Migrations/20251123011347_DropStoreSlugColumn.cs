using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConsignmentGenie.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropStoreSlugColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Organizations_StoreSlug",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "StoreSlug",
                table: "Organizations");

            migrationBuilder.UpdateData(
                table: "Organizations",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 23, 1, 13, 45, 178, DateTimeKind.Utc).AddTicks(3817), new DateTime(2025, 11, 23, 1, 13, 45, 178, DateTimeKind.Utc).AddTicks(3818) });

            migrationBuilder.UpdateData(
                table: "Providers",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 23, 1, 13, 45, 739, DateTimeKind.Utc).AddTicks(5143), new DateTime(2025, 11, 23, 1, 13, 45, 739, DateTimeKind.Utc).AddTicks(5143) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 23, 1, 13, 45, 739, DateTimeKind.Utc).AddTicks(4695), "$2a$11$F8YZ4WlpT9OSapVhXX7gge7/1StAuUA.7WqUYBeoFDDRDMhYxFnGy", new DateTime(2025, 11, 23, 1, 13, 45, 739, DateTimeKind.Utc).AddTicks(4697) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 23, 1, 13, 45, 739, DateTimeKind.Utc).AddTicks(4726), "$2a$11$F8YZ4WlpT9OSapVhXX7gge7/1StAuUA.7WqUYBeoFDDRDMhYxFnGy", new DateTime(2025, 11, 23, 1, 13, 45, 739, DateTimeKind.Utc).AddTicks(4726) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 23, 1, 13, 45, 739, DateTimeKind.Utc).AddTicks(4741), "$2a$11$F8YZ4WlpT9OSapVhXX7gge7/1StAuUA.7WqUYBeoFDDRDMhYxFnGy", new DateTime(2025, 11, 23, 1, 13, 45, 739, DateTimeKind.Utc).AddTicks(4742) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 23, 1, 13, 45, 739, DateTimeKind.Utc).AddTicks(4756), "$2a$11$F8YZ4WlpT9OSapVhXX7gge7/1StAuUA.7WqUYBeoFDDRDMhYxFnGy", new DateTime(2025, 11, 23, 1, 13, 45, 739, DateTimeKind.Utc).AddTicks(4756) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StoreSlug",
                table: "Organizations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Organizations",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "StoreSlug", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 22, 21, 35, 46, 370, DateTimeKind.Utc).AddTicks(4150), null, new DateTime(2025, 11, 22, 21, 35, 46, 370, DateTimeKind.Utc).AddTicks(4151) });

            migrationBuilder.UpdateData(
                table: "Providers",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 22, 21, 35, 46, 937, DateTimeKind.Utc).AddTicks(8545), new DateTime(2025, 11, 22, 21, 35, 46, 937, DateTimeKind.Utc).AddTicks(8546) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 22, 21, 35, 46, 937, DateTimeKind.Utc).AddTicks(8248), "$2a$11$J/fWyoF3Q8uNAiSowvrGpuHpO9iOTu7wyDElp/UvjAJv1DGof4e2K", new DateTime(2025, 11, 22, 21, 35, 46, 937, DateTimeKind.Utc).AddTicks(8249) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 22, 21, 35, 46, 937, DateTimeKind.Utc).AddTicks(8273), "$2a$11$J/fWyoF3Q8uNAiSowvrGpuHpO9iOTu7wyDElp/UvjAJv1DGof4e2K", new DateTime(2025, 11, 22, 21, 35, 46, 937, DateTimeKind.Utc).AddTicks(8274) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 22, 21, 35, 46, 937, DateTimeKind.Utc).AddTicks(8288), "$2a$11$J/fWyoF3Q8uNAiSowvrGpuHpO9iOTu7wyDElp/UvjAJv1DGof4e2K", new DateTime(2025, 11, 22, 21, 35, 46, 937, DateTimeKind.Utc).AddTicks(8289) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 22, 21, 35, 46, 937, DateTimeKind.Utc).AddTicks(8304), "$2a$11$J/fWyoF3Q8uNAiSowvrGpuHpO9iOTu7wyDElp/UvjAJv1DGof4e2K", new DateTime(2025, 11, 22, 21, 35, 46, 937, DateTimeKind.Utc).AddTicks(8304) });

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_StoreSlug",
                table: "Organizations",
                column: "StoreSlug",
                unique: true);
        }
    }
}
