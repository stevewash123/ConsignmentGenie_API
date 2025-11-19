using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConsignmentGenie.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    VerticalType = table.Column<int>(type: "integer", nullable: false),
                    Subdomain = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Settings = table.Column<string>(type: "text", nullable: true),
                    StripeCustomerId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    StripeSubscriptionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    StripePriceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SubscriptionStatus = table.Column<int>(type: "integer", nullable: false),
                    SubscriptionTier = table.Column<int>(type: "integer", nullable: false),
                    SubscriptionStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SubscriptionEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsFounderPricing = table.Column<bool>(type: "boolean", nullable: false),
                    FounderTier = table.Column<int>(type: "integer", nullable: true),
                    QuickBooksConnected = table.Column<bool>(type: "boolean", nullable: false),
                    QuickBooksRealmId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    QuickBooksAccessToken = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    QuickBooksRefreshToken = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    QuickBooksTokenExpiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    QuickBooksLastSync = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Providers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    DefaultSplitPercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    PaymentMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PaymentDetails = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Providers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Providers_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Providers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    SKU = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Price = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    CostBasis = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    Photos = table.Column<string>(type: "text", nullable: true),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ProductType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExtendedProperties = table.Column<string>(type: "text", nullable: true),
                    OverrideSplitPercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Items_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Items_Providers_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "Providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Payouts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaymentMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ItemsIncluded = table.Column<string>(type: "text", nullable: true),
                    SyncedToQuickBooks = table.Column<bool>(type: "boolean", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    QuickBooksBillId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    QuickBooksSyncFailed = table.Column<bool>(type: "boolean", nullable: false),
                    QuickBooksSyncError = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payouts_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Payouts_Providers_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "Providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    SalePrice = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    SaleDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PaymentMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ProviderSplitPercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    ProviderAmount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    ShopAmount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    SalesTaxAmount = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    TaxCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SyncedToQuickBooks = table.Column<bool>(type: "boolean", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    QuickBooksSalesReceiptId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    QuickBooksSyncFailed = table.Column<bool>(type: "boolean", nullable: false),
                    QuickBooksSyncError = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transactions_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transactions_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Transactions_Providers_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "Providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Items_OrganizationId_SKU",
                table: "Items",
                columns: new[] { "OrganizationId", "SKU" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Items_ProviderId",
                table: "Items",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_Name",
                table: "Organizations",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_Subdomain",
                table: "Organizations",
                column: "Subdomain",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payouts_OrganizationId",
                table: "Payouts",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Payouts_ProviderId_PeriodStart_PeriodEnd",
                table: "Payouts",
                columns: new[] { "ProviderId", "PeriodStart", "PeriodEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_Providers_OrganizationId_Email",
                table: "Providers",
                columns: new[] { "OrganizationId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Providers_UserId",
                table: "Providers",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ItemId",
                table: "Transactions",
                column: "ItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_OrganizationId",
                table: "Transactions",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ProviderId",
                table: "Transactions",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_SaleDate",
                table: "Transactions",
                column: "SaleDate");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_OrganizationId",
                table: "Users",
                column: "OrganizationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Payouts");

            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropTable(
                name: "Providers");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Organizations");
        }
    }
}
