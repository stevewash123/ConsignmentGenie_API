using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConsignmentGenie.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveObsoleteAgreementTemplateId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Consignors",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 4, 4, 35, 34, 516, DateTimeKind.Utc).AddTicks(938), new DateTime(2026, 2, 4, 4, 35, 34, 516, DateTimeKind.Utc).AddTicks(938) });

            migrationBuilder.UpdateData(
                table: "Organizations",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 4, 4, 35, 33, 951, DateTimeKind.Utc).AddTicks(2480), new DateTime(2026, 2, 4, 4, 35, 33, 951, DateTimeKind.Utc).AddTicks(2482) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 4, 4, 35, 34, 516, DateTimeKind.Utc).AddTicks(651), "$2a$11$YRCBRRH6MSKhysX8HOp7NudvMU25HfLCP4u8O1RWr2/ikcjXUEmJu", new DateTime(2026, 2, 4, 4, 35, 34, 516, DateTimeKind.Utc).AddTicks(696) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 4, 4, 35, 34, 516, DateTimeKind.Utc).AddTicks(724), "$2a$11$YRCBRRH6MSKhysX8HOp7NudvMU25HfLCP4u8O1RWr2/ikcjXUEmJu", new DateTime(2026, 2, 4, 4, 35, 34, 516, DateTimeKind.Utc).AddTicks(725) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 4, 4, 35, 34, 516, DateTimeKind.Utc).AddTicks(740), "$2a$11$YRCBRRH6MSKhysX8HOp7NudvMU25HfLCP4u8O1RWr2/ikcjXUEmJu", new DateTime(2026, 2, 4, 4, 35, 34, 516, DateTimeKind.Utc).AddTicks(740) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 4, 4, 35, 34, 516, DateTimeKind.Utc).AddTicks(758), "$2a$11$YRCBRRH6MSKhysX8HOp7NudvMU25HfLCP4u8O1RWr2/ikcjXUEmJu", new DateTime(2026, 2, 4, 4, 35, 34, 516, DateTimeKind.Utc).AddTicks(759) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Consignors",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 3, 12, 23, 13, 338, DateTimeKind.Utc).AddTicks(3062), new DateTime(2026, 2, 3, 12, 23, 13, 338, DateTimeKind.Utc).AddTicks(3063) });

            migrationBuilder.UpdateData(
                table: "Organizations",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 3, 12, 23, 12, 788, DateTimeKind.Utc).AddTicks(4954), new DateTime(2026, 2, 3, 12, 23, 12, 788, DateTimeKind.Utc).AddTicks(4955) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 3, 12, 23, 13, 338, DateTimeKind.Utc).AddTicks(2770), "$2a$11$.PI1w1c1xOtGCVY8LPfFo.J7JvVHWfKfBS0WfPsg6PqKYIhjKDQyy", new DateTime(2026, 2, 3, 12, 23, 13, 338, DateTimeKind.Utc).AddTicks(2819) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 3, 12, 23, 13, 338, DateTimeKind.Utc).AddTicks(2847), "$2a$11$.PI1w1c1xOtGCVY8LPfFo.J7JvVHWfKfBS0WfPsg6PqKYIhjKDQyy", new DateTime(2026, 2, 3, 12, 23, 13, 338, DateTimeKind.Utc).AddTicks(2847) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 3, 12, 23, 13, 338, DateTimeKind.Utc).AddTicks(2861), "$2a$11$.PI1w1c1xOtGCVY8LPfFo.J7JvVHWfKfBS0WfPsg6PqKYIhjKDQyy", new DateTime(2026, 2, 3, 12, 23, 13, 338, DateTimeKind.Utc).AddTicks(2862) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 3, 12, 23, 13, 338, DateTimeKind.Utc).AddTicks(2874), "$2a$11$.PI1w1c1xOtGCVY8LPfFo.J7JvVHWfKfBS0WfPsg6PqKYIhjKDQyy", new DateTime(2026, 2, 3, 12, 23, 13, 338, DateTimeKind.Utc).AddTicks(2875) });
        }
    }
}
