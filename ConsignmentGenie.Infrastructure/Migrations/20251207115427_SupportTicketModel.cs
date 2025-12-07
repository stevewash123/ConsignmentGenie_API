using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConsignmentGenie.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SupportTicketModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProviderInvitations");

            migrationBuilder.RenameColumn(
                name: "Consignor",
                table: "PaymentGatewayConnections",
                newName: "Provider");

            migrationBuilder.RenameIndex(
                name: "IX_PaymentGatewayConnections_OrganizationId_Consignor_IsActive",
                table: "PaymentGatewayConnections",
                newName: "IX_PaymentGatewayConnections_OrganizationId_Provider_IsActive");

            migrationBuilder.RenameColumn(
                name: "ShowProviderNames",
                table: "ClerkPermissions",
                newName: "ShowConsignorNames");

            migrationBuilder.AddColumn<string>(
                name: "BusinessSettings",
                table: "Organizations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StorefrontSettings",
                table: "Organizations",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ConsignorInvitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvitedById = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
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
                    table.PrimaryKey("PK_ConsignorInvitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsignorInvitations_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConsignorInvitations_Users_InvitedById",
                        column: x => x.InvitedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SupportTickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    AssignedTo = table.Column<int>(type: "integer", nullable: false),
                    Subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    SubmittedById = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportTickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupportTickets_Users_SubmittedById",
                        column: x => x.SubmittedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "Consignors",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 12, 7, 11, 54, 25, 571, DateTimeKind.Utc).AddTicks(6102), new DateTime(2025, 12, 7, 11, 54, 25, 571, DateTimeKind.Utc).AddTicks(6103) });

            migrationBuilder.UpdateData(
                table: "Organizations",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "BusinessSettings", "CreatedAt", "StorefrontSettings", "UpdatedAt" },
                values: new object[] { null, new DateTime(2025, 12, 7, 11, 54, 24, 983, DateTimeKind.Utc).AddTicks(905), null, new DateTime(2025, 12, 7, 11, 54, 24, 983, DateTimeKind.Utc).AddTicks(908) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 12, 7, 11, 54, 25, 571, DateTimeKind.Utc).AddTicks(5807), "$2a$11$QbSCoa4g4g1hPkZNUrD08ef0EEGb0LafwKVPlvqcRKxEYQeZ/lTli", new DateTime(2025, 12, 7, 11, 54, 25, 571, DateTimeKind.Utc).AddTicks(5845) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 12, 7, 11, 54, 25, 571, DateTimeKind.Utc).AddTicks(5877), "$2a$11$QbSCoa4g4g1hPkZNUrD08ef0EEGb0LafwKVPlvqcRKxEYQeZ/lTli", new DateTime(2025, 12, 7, 11, 54, 25, 571, DateTimeKind.Utc).AddTicks(5877) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 12, 7, 11, 54, 25, 571, DateTimeKind.Utc).AddTicks(5895), "$2a$11$QbSCoa4g4g1hPkZNUrD08ef0EEGb0LafwKVPlvqcRKxEYQeZ/lTli", new DateTime(2025, 12, 7, 11, 54, 25, 571, DateTimeKind.Utc).AddTicks(5895) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 12, 7, 11, 54, 25, 571, DateTimeKind.Utc).AddTicks(5913), "$2a$11$QbSCoa4g4g1hPkZNUrD08ef0EEGb0LafwKVPlvqcRKxEYQeZ/lTli", new DateTime(2025, 12, 7, 11, 54, 25, 571, DateTimeKind.Utc).AddTicks(5913) });

            migrationBuilder.CreateIndex(
                name: "IX_ConsignorInvitations_ExpiresAt",
                table: "ConsignorInvitations",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ConsignorInvitations_InvitedById",
                table: "ConsignorInvitations",
                column: "InvitedById");

            migrationBuilder.CreateIndex(
                name: "IX_ConsignorInvitations_OrganizationId",
                table: "ConsignorInvitations",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsignorInvitations_OrganizationId_Email",
                table: "ConsignorInvitations",
                columns: new[] { "OrganizationId", "Email" });

            migrationBuilder.CreateIndex(
                name: "IX_ConsignorInvitations_Status",
                table: "ConsignorInvitations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ConsignorInvitations_Token",
                table: "ConsignorInvitations",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_AssignedTo",
                table: "SupportTickets",
                column: "AssignedTo");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_Category",
                table: "SupportTickets",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_CreatedAt",
                table: "SupportTickets",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_Status",
                table: "SupportTickets",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_SubmittedById",
                table: "SupportTickets",
                column: "SubmittedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsignorInvitations");

            migrationBuilder.DropTable(
                name: "SupportTickets");

            migrationBuilder.DropColumn(
                name: "BusinessSettings",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "StorefrontSettings",
                table: "Organizations");

            migrationBuilder.RenameColumn(
                name: "Provider",
                table: "PaymentGatewayConnections",
                newName: "Consignor");

            migrationBuilder.RenameIndex(
                name: "IX_PaymentGatewayConnections_OrganizationId_Provider_IsActive",
                table: "PaymentGatewayConnections",
                newName: "IX_PaymentGatewayConnections_OrganizationId_Consignor_IsActive");

            migrationBuilder.RenameColumn(
                name: "ShowConsignorNames",
                table: "ClerkPermissions",
                newName: "ShowProviderNames");

            migrationBuilder.CreateTable(
                name: "ProviderInvitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InvitedById = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsUsed = table.Column<bool>(type: "boolean", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Token = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderInvitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProviderInvitations_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProviderInvitations_Users_InvitedById",
                        column: x => x.InvitedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

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
                values: new object[] { new DateTime(2025, 12, 3, 2, 34, 1, 497, DateTimeKind.Utc).AddTicks(8074), new DateTime(2025, 12, 3, 2, 34, 1, 497, DateTimeKind.Utc).AddTicks(8075) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 12, 3, 2, 34, 2, 45, DateTimeKind.Utc).AddTicks(1718), "$2a$11$1JpmhzU/QWG./YJ.ZlIVaueakB9lslVzMO0drbnWly7YgOvXfUjkO", new DateTime(2025, 12, 3, 2, 34, 2, 45, DateTimeKind.Utc).AddTicks(1755) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 12, 3, 2, 34, 2, 45, DateTimeKind.Utc).AddTicks(1782), "$2a$11$1JpmhzU/QWG./YJ.ZlIVaueakB9lslVzMO0drbnWly7YgOvXfUjkO", new DateTime(2025, 12, 3, 2, 34, 2, 45, DateTimeKind.Utc).AddTicks(1783) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 12, 3, 2, 34, 2, 45, DateTimeKind.Utc).AddTicks(1797), "$2a$11$1JpmhzU/QWG./YJ.ZlIVaueakB9lslVzMO0drbnWly7YgOvXfUjkO", new DateTime(2025, 12, 3, 2, 34, 2, 45, DateTimeKind.Utc).AddTicks(1798) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 12, 3, 2, 34, 2, 45, DateTimeKind.Utc).AddTicks(1812), "$2a$11$1JpmhzU/QWG./YJ.ZlIVaueakB9lslVzMO0drbnWly7YgOvXfUjkO", new DateTime(2025, 12, 3, 2, 34, 2, 45, DateTimeKind.Utc).AddTicks(1812) });

            migrationBuilder.CreateIndex(
                name: "IX_ProviderInvitations_ExpiresAt",
                table: "ProviderInvitations",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderInvitations_InvitedById",
                table: "ProviderInvitations",
                column: "InvitedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderInvitations_OrganizationId",
                table: "ProviderInvitations",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderInvitations_OrganizationId_Email",
                table: "ProviderInvitations",
                columns: new[] { "OrganizationId", "Email" });

            migrationBuilder.CreateIndex(
                name: "IX_ProviderInvitations_Status",
                table: "ProviderInvitations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderInvitations_Token",
                table: "ProviderInvitations",
                column: "Token",
                unique: true);
        }
    }
}
