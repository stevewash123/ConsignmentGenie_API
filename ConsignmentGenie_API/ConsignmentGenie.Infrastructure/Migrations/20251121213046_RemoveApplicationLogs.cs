using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConsignmentGenie.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveApplicationLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationLogs");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApplicationLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Exception = table.Column<string>(type: "text", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    Level = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    Method = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Properties = table.Column<string>(type: "text", nullable: true),
                    RequestPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationLogs_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ApplicationLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.UpdateData(
                table: "Organizations",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 21, 20, 44, 51, 446, DateTimeKind.Utc).AddTicks(3773), new DateTime(2025, 11, 21, 20, 44, 51, 446, DateTimeKind.Utc).AddTicks(3774) });

            migrationBuilder.UpdateData(
                table: "Providers",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 21, 20, 44, 51, 652, DateTimeKind.Utc).AddTicks(3145), new DateTime(2025, 11, 21, 20, 44, 51, 652, DateTimeKind.Utc).AddTicks(3145) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 21, 20, 44, 51, 652, DateTimeKind.Utc).AddTicks(2530), "$2a$11$vNEPq3UEKNgBYyewNN.UAetPTEb9UT6blRC0ExixR1uBNzz0tstmG", new DateTime(2025, 11, 21, 20, 44, 51, 652, DateTimeKind.Utc).AddTicks(2531) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 21, 20, 44, 51, 652, DateTimeKind.Utc).AddTicks(2559), "$2a$11$vNEPq3UEKNgBYyewNN.UAetPTEb9UT6blRC0ExixR1uBNzz0tstmG", new DateTime(2025, 11, 21, 20, 44, 51, 652, DateTimeKind.Utc).AddTicks(2559) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 21, 20, 44, 51, 652, DateTimeKind.Utc).AddTicks(2572), "$2a$11$vNEPq3UEKNgBYyewNN.UAetPTEb9UT6blRC0ExixR1uBNzz0tstmG", new DateTime(2025, 11, 21, 20, 44, 51, 652, DateTimeKind.Utc).AddTicks(2593) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 21, 20, 44, 51, 652, DateTimeKind.Utc).AddTicks(2805), "$2a$11$vNEPq3UEKNgBYyewNN.UAetPTEb9UT6blRC0ExixR1uBNzz0tstmG", new DateTime(2025, 11, 21, 20, 44, 51, 652, DateTimeKind.Utc).AddTicks(2806) });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationLogs_Level",
                table: "ApplicationLogs",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationLogs_OrganizationId",
                table: "ApplicationLogs",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationLogs_OrganizationId_Timestamp",
                table: "ApplicationLogs",
                columns: new[] { "OrganizationId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationLogs_Timestamp",
                table: "ApplicationLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationLogs_UserId",
                table: "ApplicationLogs",
                column: "UserId");
        }
    }
}
