using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConsignmentGenie.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PendingConsolidatedNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PendingConsolidatedNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsignorId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    NotificationType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ItemIds = table.Column<string>(type: "jsonb", nullable: false),
                    HangfireJobId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ScheduledFor = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessingStartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingConsolidatedNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PendingConsolidatedNotifications_Consignors_ConsignorId",
                        column: x => x.ConsignorId,
                        principalTable: "Consignors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PendingConsolidatedNotifications_Organizations_Organization~",
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
                values: new object[] { new DateTime(2026, 2, 6, 0, 59, 36, 39, DateTimeKind.Utc).AddTicks(6586), new DateTime(2026, 2, 6, 0, 59, 36, 39, DateTimeKind.Utc).AddTicks(6587) });

            migrationBuilder.UpdateData(
                table: "Organizations",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 6, 0, 59, 35, 491, DateTimeKind.Utc).AddTicks(9357), new DateTime(2026, 2, 6, 0, 59, 35, 491, DateTimeKind.Utc).AddTicks(9358) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 6, 0, 59, 36, 39, DateTimeKind.Utc).AddTicks(6297), "$2a$11$2IgW0ovHKDGE3yG9xPomOe9UOYy10w3KkciLaAD1RWMmQZH5opOfy", new DateTime(2026, 2, 6, 0, 59, 36, 39, DateTimeKind.Utc).AddTicks(6339) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 6, 0, 59, 36, 39, DateTimeKind.Utc).AddTicks(6369), "$2a$11$2IgW0ovHKDGE3yG9xPomOe9UOYy10w3KkciLaAD1RWMmQZH5opOfy", new DateTime(2026, 2, 6, 0, 59, 36, 39, DateTimeKind.Utc).AddTicks(6369) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 6, 0, 59, 36, 39, DateTimeKind.Utc).AddTicks(6385), "$2a$11$2IgW0ovHKDGE3yG9xPomOe9UOYy10w3KkciLaAD1RWMmQZH5opOfy", new DateTime(2026, 2, 6, 0, 59, 36, 39, DateTimeKind.Utc).AddTicks(6385) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 6, 0, 59, 36, 39, DateTimeKind.Utc).AddTicks(6401), "$2a$11$2IgW0ovHKDGE3yG9xPomOe9UOYy10w3KkciLaAD1RWMmQZH5opOfy", new DateTime(2026, 2, 6, 0, 59, 36, 39, DateTimeKind.Utc).AddTicks(6401) });

            migrationBuilder.CreateIndex(
                name: "IX_PendingConsolidatedNotifications_ConsignorId",
                table: "PendingConsolidatedNotifications",
                column: "ConsignorId");

            migrationBuilder.CreateIndex(
                name: "IX_PendingConsolidatedNotifications_OrganizationId",
                table: "PendingConsolidatedNotifications",
                column: "OrganizationId");

            // Unique constraint: one pending notification per consignor per type (unless being processed)
            migrationBuilder.CreateIndex(
                name: "IX_PendingConsolidatedNotifications_Unique_ConsignorOrgType",
                table: "PendingConsolidatedNotifications",
                columns: new[] { "ConsignorId", "OrganizationId", "NotificationType" },
                unique: true,
                filter: "\"SentAt\" IS NULL AND \"ProcessingStartedAt\" IS NULL");

            // Index for scheduled jobs cleanup
            migrationBuilder.CreateIndex(
                name: "IX_PendingConsolidatedNotifications_ScheduledFor",
                table: "PendingConsolidatedNotifications",
                column: "ScheduledFor",
                filter: "\"SentAt\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PendingConsolidatedNotifications");

            migrationBuilder.UpdateData(
                table: "Consignors",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 5, 22, 57, 34, 745, DateTimeKind.Utc).AddTicks(236), new DateTime(2026, 2, 5, 22, 57, 34, 745, DateTimeKind.Utc).AddTicks(236) });

            migrationBuilder.UpdateData(
                table: "Organizations",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 5, 22, 57, 34, 216, DateTimeKind.Utc).AddTicks(6942), new DateTime(2026, 2, 5, 22, 57, 34, 216, DateTimeKind.Utc).AddTicks(6943) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 5, 22, 57, 34, 744, DateTimeKind.Utc).AddTicks(9988), "$2a$11$Y23PX6BCh2nnfs75/JYX0.gbGQAIIzISOguIJtZp/es54bdJOhbou", new DateTime(2026, 2, 5, 22, 57, 34, 745, DateTimeKind.Utc).AddTicks(20) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 5, 22, 57, 34, 745, DateTimeKind.Utc).AddTicks(48), "$2a$11$Y23PX6BCh2nnfs75/JYX0.gbGQAIIzISOguIJtZp/es54bdJOhbou", new DateTime(2026, 2, 5, 22, 57, 34, 745, DateTimeKind.Utc).AddTicks(48) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 5, 22, 57, 34, 745, DateTimeKind.Utc).AddTicks(63), "$2a$11$Y23PX6BCh2nnfs75/JYX0.gbGQAIIzISOguIJtZp/es54bdJOhbou", new DateTime(2026, 2, 5, 22, 57, 34, 745, DateTimeKind.Utc).AddTicks(63) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 5, 22, 57, 34, 745, DateTimeKind.Utc).AddTicks(76), "$2a$11$Y23PX6BCh2nnfs75/JYX0.gbGQAIIzISOguIJtZp/es54bdJOhbou", new DateTime(2026, 2, 5, 22, 57, 34, 745, DateTimeKind.Utc).AddTicks(77) });
        }
    }
}
