using Microsoft.EntityFrameworkCore.Migrations;

namespace ConsignmentGenie.Infrastructure.Migrations;

public partial class AddShopperPhase1Features : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Add Slug column to Organizations
        migrationBuilder.AddColumn<string>(
            name: "Slug",
            table: "Organizations",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);

        // Create unique index on Slug
        migrationBuilder.CreateIndex(
            name: "IX_Organizations_Slug",
            table: "Organizations",
            column: "Slug",
            unique: true);

        // Create Shoppers table
        migrationBuilder.CreateTable(
            name: "Shoppers",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                ShippingAddress1 = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                ShippingAddress2 = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                ShippingCity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                ShippingState = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                ShippingZip = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                EmailNotifications = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                LastLoginAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Shoppers", x => x.Id);
                table.ForeignKey(
                    name: "FK_Shoppers_Organizations_OrganizationId",
                    column: x => x.OrganizationId,
                    principalTable: "Organizations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Shoppers_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        // Create GuestCheckouts table
        migrationBuilder.CreateTable(
            name: "GuestCheckouts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                SessionToken = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                ExpiresAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_GuestCheckouts", x => x.Id);
                table.ForeignKey(
                    name: "FK_GuestCheckouts_Organizations_OrganizationId",
                    column: x => x.OrganizationId,
                    principalTable: "Organizations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        // Create indexes for Shoppers
        migrationBuilder.CreateIndex(
            name: "IX_Shoppers_OrganizationId",
            table: "Shoppers",
            column: "OrganizationId");

        migrationBuilder.CreateIndex(
            name: "IX_Shoppers_UserId",
            table: "Shoppers",
            column: "UserId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Shoppers_OrganizationId_Email",
            table: "Shoppers",
            columns: new[] { "OrganizationId", "Email" },
            unique: true);

        // Create indexes for GuestCheckouts
        migrationBuilder.CreateIndex(
            name: "IX_GuestCheckouts_OrganizationId",
            table: "GuestCheckouts",
            column: "OrganizationId");

        migrationBuilder.CreateIndex(
            name: "IX_GuestCheckouts_SessionToken",
            table: "GuestCheckouts",
            column: "SessionToken",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_GuestCheckouts_ExpiresAt",
            table: "GuestCheckouts",
            column: "ExpiresAt");

        // Generate slugs for existing organizations
        migrationBuilder.Sql(@"
            UPDATE ""Organizations""
            SET ""Slug"" = CASE
                WHEN ""Name"" IS NOT NULL THEN
                    LOWER(
                        REGEXP_REPLACE(
                            REGEXP_REPLACE(
                                REGEXP_REPLACE(""Name"", '[^a-zA-Z0-9\s-]', '', 'g'),
                                '\s+', '-', 'g'
                            ),
                            '-+', '-', 'g'
                        )
                    )
                ELSE 'shop-' || ""Id""::text
            END
            WHERE ""Slug"" IS NULL;
        ");

        // Handle duplicate slugs by appending numbers
        migrationBuilder.Sql(@"
            WITH numbered_orgs AS (
                SELECT ""Id"", ""Slug"",
                       ROW_NUMBER() OVER (PARTITION BY ""Slug"" ORDER BY ""CreatedAt"") as rn
                FROM ""Organizations""
            )
            UPDATE ""Organizations""
            SET ""Slug"" = CASE
                WHEN numbered_orgs.rn > 1 THEN numbered_orgs.""Slug"" || '-' || numbered_orgs.rn::text
                ELSE numbered_orgs.""Slug""
            END
            FROM numbered_orgs
            WHERE ""Organizations"".""Id"" = numbered_orgs.""Id""
              AND numbered_orgs.rn > 1;
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Drop tables
        migrationBuilder.DropTable(name: "Shoppers");
        migrationBuilder.DropTable(name: "GuestCheckouts");

        // Drop Organizations Slug column and index
        migrationBuilder.DropIndex(
            name: "IX_Organizations_Slug",
            table: "Organizations");

        migrationBuilder.DropColumn(
            name: "Slug",
            table: "Organizations");
    }
}