using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConsignmentGenie.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RegistrationAndApprovalFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ApprovalStatus",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedBy",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectedReason",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StoreCode",
                table: "Organizations",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "StoreCodeEnabled",
                table: "Organizations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Suggestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserEmail = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UserName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    IsProcessed = table.Column<bool>(type: "boolean", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AdminNotes = table.Column<string>(type: "text", nullable: true),
                    EmailSent = table.Column<bool>(type: "boolean", nullable: false),
                    EmailSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suggestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Suggestions_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Suggestions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserNotificationPreferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    NotificationType = table.Column<int>(type: "integer", nullable: false),
                    EmailEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    SmsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    SlackEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    PushEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    SlackUserId = table.Column<string>(type: "text", nullable: true),
                    InstantDelivery = table.Column<bool>(type: "boolean", nullable: false),
                    QuietHoursStart = table.Column<TimeSpan>(type: "interval", nullable: true),
                    QuietHoursEnd = table.Column<TimeSpan>(type: "interval", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNotificationPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserNotificationPreferences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Organizations",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "StoreCode", "StoreCodeEnabled", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 22, 13, 19, 46, 788, DateTimeKind.Utc).AddTicks(3081), null, true, new DateTime(2025, 11, 22, 13, 19, 46, 788, DateTimeKind.Utc).AddTicks(3082) });

            migrationBuilder.UpdateData(
                table: "Providers",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 22, 13, 19, 47, 3, DateTimeKind.Utc).AddTicks(8963), new DateTime(2025, 11, 22, 13, 19, 47, 3, DateTimeKind.Utc).AddTicks(8963) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "ApprovalStatus", "ApprovedAt", "ApprovedBy", "CreatedAt", "FullName", "PasswordHash", "Phone", "RejectedReason", "UpdatedAt" },
                values: new object[] { 1, null, null, new DateTime(2025, 11, 22, 13, 19, 47, 3, DateTimeKind.Utc).AddTicks(8657), null, "$2a$11$CGLc9CD5/XAJqp1eCd67yelz9daNNKGQM9maAEEHIJVek/bBfTqGi", null, null, new DateTime(2025, 11, 22, 13, 19, 47, 3, DateTimeKind.Utc).AddTicks(8666) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "ApprovalStatus", "ApprovedAt", "ApprovedBy", "CreatedAt", "FullName", "PasswordHash", "Phone", "RejectedReason", "UpdatedAt" },
                values: new object[] { 1, null, null, new DateTime(2025, 11, 22, 13, 19, 47, 3, DateTimeKind.Utc).AddTicks(8693), null, "$2a$11$CGLc9CD5/XAJqp1eCd67yelz9daNNKGQM9maAEEHIJVek/bBfTqGi", null, null, new DateTime(2025, 11, 22, 13, 19, 47, 3, DateTimeKind.Utc).AddTicks(8693) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "ApprovalStatus", "ApprovedAt", "ApprovedBy", "CreatedAt", "FullName", "PasswordHash", "Phone", "RejectedReason", "UpdatedAt" },
                values: new object[] { 1, null, null, new DateTime(2025, 11, 22, 13, 19, 47, 3, DateTimeKind.Utc).AddTicks(8710), null, "$2a$11$CGLc9CD5/XAJqp1eCd67yelz9daNNKGQM9maAEEHIJVek/bBfTqGi", null, null, new DateTime(2025, 11, 22, 13, 19, 47, 3, DateTimeKind.Utc).AddTicks(8711) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "ApprovalStatus", "ApprovedAt", "ApprovedBy", "CreatedAt", "FullName", "PasswordHash", "Phone", "RejectedReason", "UpdatedAt" },
                values: new object[] { 1, null, null, new DateTime(2025, 11, 22, 13, 19, 47, 3, DateTimeKind.Utc).AddTicks(8726), null, "$2a$11$CGLc9CD5/XAJqp1eCd67yelz9daNNKGQM9maAEEHIJVek/bBfTqGi", null, null, new DateTime(2025, 11, 22, 13, 19, 47, 3, DateTimeKind.Utc).AddTicks(8727) });

            migrationBuilder.CreateIndex(
                name: "IX_Suggestions_CreatedAt",
                table: "Suggestions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Suggestions_IsProcessed",
                table: "Suggestions",
                column: "IsProcessed");

            migrationBuilder.CreateIndex(
                name: "IX_Suggestions_OrganizationId",
                table: "Suggestions",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Suggestions_Type",
                table: "Suggestions",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Suggestions_UserId",
                table: "Suggestions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotificationPreferences_NotificationType",
                table: "UserNotificationPreferences",
                column: "NotificationType");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotificationPreferences_UserId",
                table: "UserNotificationPreferences",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotificationPreferences_UserId_NotificationType",
                table: "UserNotificationPreferences",
                columns: new[] { "UserId", "NotificationType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Suggestions");

            migrationBuilder.DropTable(
                name: "UserNotificationPreferences");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FullName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RejectedReason",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "StoreCode",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "StoreCodeEnabled",
                table: "Organizations");

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
        }
    }
}
