using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConsignmentGenie.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PaymentGatewayConnections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PaymentGatewayConnections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "text", nullable: false),
                    ConnectionName = table.Column<string>(type: "text", nullable: false),
                    EncryptedConfig = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentGatewayConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentGatewayConnections_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentGatewayConnections_OrganizationId",
                table: "PaymentGatewayConnections",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentGatewayConnections_OrganizationId_IsDefault",
                table: "PaymentGatewayConnections",
                columns: new[] { "OrganizationId", "IsDefault" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentGatewayConnections_OrganizationId_Provider_IsActive",
                table: "PaymentGatewayConnections",
                columns: new[] { "OrganizationId", "Provider", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentGatewayConnections");
        }
    }
}
