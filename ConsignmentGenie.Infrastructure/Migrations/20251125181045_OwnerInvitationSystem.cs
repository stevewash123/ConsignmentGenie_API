using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConsignmentGenie.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OwnerInvitationSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OwnerInvitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InvitedById = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Token = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OwnerInvitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OwnerInvitations_Users_InvitedById",
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
                values: new object[] { new DateTime(2025, 11, 25, 18, 10, 43, 185, DateTimeKind.Utc).AddTicks(4164), new DateTime(2025, 11, 25, 18, 10, 43, 185, DateTimeKind.Utc).AddTicks(4165) });

            migrationBuilder.UpdateData(
                table: "Providers",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 25, 18, 10, 43, 734, DateTimeKind.Utc).AddTicks(9358), new DateTime(2025, 11, 25, 18, 10, 43, 734, DateTimeKind.Utc).AddTicks(9358) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 25, 18, 10, 43, 734, DateTimeKind.Utc).AddTicks(9129), "$2a$11$G2cWPDE69h1YXfcJYCFZGeATWoiF1uED58WKAab0xs2h9An1DtqDm", new DateTime(2025, 11, 25, 18, 10, 43, 734, DateTimeKind.Utc).AddTicks(9141) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 25, 18, 10, 43, 734, DateTimeKind.Utc).AddTicks(9166), "$2a$11$G2cWPDE69h1YXfcJYCFZGeATWoiF1uED58WKAab0xs2h9An1DtqDm", new DateTime(2025, 11, 25, 18, 10, 43, 734, DateTimeKind.Utc).AddTicks(9167) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 25, 18, 10, 43, 734, DateTimeKind.Utc).AddTicks(9182), "$2a$11$G2cWPDE69h1YXfcJYCFZGeATWoiF1uED58WKAab0xs2h9An1DtqDm", new DateTime(2025, 11, 25, 18, 10, 43, 734, DateTimeKind.Utc).AddTicks(9183) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 25, 18, 10, 43, 734, DateTimeKind.Utc).AddTicks(9199), "$2a$11$G2cWPDE69h1YXfcJYCFZGeATWoiF1uED58WKAab0xs2h9An1DtqDm", new DateTime(2025, 11, 25, 18, 10, 43, 734, DateTimeKind.Utc).AddTicks(9200) });

            migrationBuilder.CreateIndex(
                name: "IX_OwnerInvitations_Email",
                table: "OwnerInvitations",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_OwnerInvitations_ExpiresAt",
                table: "OwnerInvitations",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_OwnerInvitations_InvitedById",
                table: "OwnerInvitations",
                column: "InvitedById");

            migrationBuilder.CreateIndex(
                name: "IX_OwnerInvitations_Status",
                table: "OwnerInvitations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_OwnerInvitations_Token",
                table: "OwnerInvitations",
                column: "Token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OwnerInvitations");

            migrationBuilder.UpdateData(
                table: "Organizations",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 25, 17, 59, 5, 795, DateTimeKind.Utc).AddTicks(7203), new DateTime(2025, 11, 25, 17, 59, 5, 795, DateTimeKind.Utc).AddTicks(7204) });

            migrationBuilder.UpdateData(
                table: "Providers",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 25, 17, 59, 6, 369, DateTimeKind.Utc).AddTicks(6904), new DateTime(2025, 11, 25, 17, 59, 6, 369, DateTimeKind.Utc).AddTicks(6905) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 25, 17, 59, 6, 369, DateTimeKind.Utc).AddTicks(6622), "$2a$11$FeMe6QAnAKVjvVksiRB.AukW3Ui2FZxGvT2BhtcBvP5iSeNfm3ULy", new DateTime(2025, 11, 25, 17, 59, 6, 369, DateTimeKind.Utc).AddTicks(6654) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 25, 17, 59, 6, 369, DateTimeKind.Utc).AddTicks(6684), "$2a$11$FeMe6QAnAKVjvVksiRB.AukW3Ui2FZxGvT2BhtcBvP5iSeNfm3ULy", new DateTime(2025, 11, 25, 17, 59, 6, 369, DateTimeKind.Utc).AddTicks(6684) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 25, 17, 59, 6, 369, DateTimeKind.Utc).AddTicks(6702), "$2a$11$FeMe6QAnAKVjvVksiRB.AukW3Ui2FZxGvT2BhtcBvP5iSeNfm3ULy", new DateTime(2025, 11, 25, 17, 59, 6, 369, DateTimeKind.Utc).AddTicks(6703) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 25, 17, 59, 6, 369, DateTimeKind.Utc).AddTicks(6719), "$2a$11$FeMe6QAnAKVjvVksiRB.AukW3Ui2FZxGvT2BhtcBvP5iSeNfm3ULy", new DateTime(2025, 11, 25, 17, 59, 6, 369, DateTimeKind.Utc).AddTicks(6720) });
        }
    }
}
