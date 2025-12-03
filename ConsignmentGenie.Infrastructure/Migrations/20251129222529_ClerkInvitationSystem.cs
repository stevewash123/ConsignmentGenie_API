using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConsignmentGenie.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ClerkInvitationSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClerkInvitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvitedById = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Token = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsUsed = table.Column<bool>(type: "boolean", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClerkInvitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClerkInvitations_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClerkInvitations_Users_InvitedById",
                        column: x => x.InvitedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.UpdateData(
                table: "Organizations",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 29, 22, 25, 27, 505, DateTimeKind.Utc).AddTicks(7769), new DateTime(2025, 11, 29, 22, 25, 27, 505, DateTimeKind.Utc).AddTicks(7770) });

            migrationBuilder.UpdateData(
                table: "Consignors",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 29, 22, 25, 28, 42, DateTimeKind.Utc).AddTicks(9655), new DateTime(2025, 11, 29, 22, 25, 28, 42, DateTimeKind.Utc).AddTicks(9656) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 29, 22, 25, 28, 42, DateTimeKind.Utc).AddTicks(9379), "$2a$11$ao7rj/C16LmCZ9s74B1mcubANh82xO4bZd6aO7hEFb1iImI1QU8ZO", new DateTime(2025, 11, 29, 22, 25, 28, 42, DateTimeKind.Utc).AddTicks(9416) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 29, 22, 25, 28, 42, DateTimeKind.Utc).AddTicks(9447), "$2a$11$ao7rj/C16LmCZ9s74B1mcubANh82xO4bZd6aO7hEFb1iImI1QU8ZO", new DateTime(2025, 11, 29, 22, 25, 28, 42, DateTimeKind.Utc).AddTicks(9448) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 29, 22, 25, 28, 42, DateTimeKind.Utc).AddTicks(9463), "$2a$11$ao7rj/C16LmCZ9s74B1mcubANh82xO4bZd6aO7hEFb1iImI1QU8ZO", new DateTime(2025, 11, 29, 22, 25, 28, 42, DateTimeKind.Utc).AddTicks(9464) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 29, 22, 25, 28, 42, DateTimeKind.Utc).AddTicks(9480), "$2a$11$ao7rj/C16LmCZ9s74B1mcubANh82xO4bZd6aO7hEFb1iImI1QU8ZO", new DateTime(2025, 11, 29, 22, 25, 28, 42, DateTimeKind.Utc).AddTicks(9481) });

            migrationBuilder.CreateIndex(
                name: "IX_ClerkInvitations_ExpiresAt",
                table: "ClerkInvitations",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ClerkInvitations_InvitedById",
                table: "ClerkInvitations",
                column: "InvitedById");

            migrationBuilder.CreateIndex(
                name: "IX_ClerkInvitations_OrganizationId",
                table: "ClerkInvitations",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ClerkInvitations_OrganizationId_Email",
                table: "ClerkInvitations",
                columns: new[] { "OrganizationId", "Email" });

            migrationBuilder.CreateIndex(
                name: "IX_ClerkInvitations_Status",
                table: "ClerkInvitations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ClerkInvitations_Token",
                table: "ClerkInvitations",
                column: "Token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClerkInvitations");

            migrationBuilder.UpdateData(
                table: "Organizations",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 29, 22, 19, 58, 821, DateTimeKind.Utc).AddTicks(8413), new DateTime(2025, 11, 29, 22, 19, 58, 821, DateTimeKind.Utc).AddTicks(8414) });

            migrationBuilder.UpdateData(
                table: "Consignors",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 29, 22, 19, 59, 351, DateTimeKind.Utc).AddTicks(8245), new DateTime(2025, 11, 29, 22, 19, 59, 351, DateTimeKind.Utc).AddTicks(8246) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 29, 22, 19, 59, 351, DateTimeKind.Utc).AddTicks(7969), "$2a$11$nv1e.TRcEATUJ/txPQYRs.LfxE18Zgywc71LC5lCvKaVGdSsWkyUm", new DateTime(2025, 11, 29, 22, 19, 59, 351, DateTimeKind.Utc).AddTicks(8006) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 29, 22, 19, 59, 351, DateTimeKind.Utc).AddTicks(8034), "$2a$11$nv1e.TRcEATUJ/txPQYRs.LfxE18Zgywc71LC5lCvKaVGdSsWkyUm", new DateTime(2025, 11, 29, 22, 19, 59, 351, DateTimeKind.Utc).AddTicks(8035) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 29, 22, 19, 59, 351, DateTimeKind.Utc).AddTicks(8051), "$2a$11$nv1e.TRcEATUJ/txPQYRs.LfxE18Zgywc71LC5lCvKaVGdSsWkyUm", new DateTime(2025, 11, 29, 22, 19, 59, 351, DateTimeKind.Utc).AddTicks(8051) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 29, 22, 19, 59, 351, DateTimeKind.Utc).AddTicks(8068), "$2a$11$nv1e.TRcEATUJ/txPQYRs.LfxE18Zgywc71LC5lCvKaVGdSsWkyUm", new DateTime(2025, 11, 29, 22, 19, 59, 351, DateTimeKind.Utc).AddTicks(8068) });
        }
    }
}
