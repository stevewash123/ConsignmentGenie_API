using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ConsignmentGenie.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SetupChecklistSupport : Migration
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
                    Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Settings = table.Column<string>(type: "text", nullable: true),
                    BusinessSettings = table.Column<string>(type: "text", nullable: true),
                    StorefrontSettings = table.Column<string>(type: "text", nullable: true),
                    SalesSettings = table.Column<string>(type: "text", nullable: true),
                    ConsignorPermissions = table.Column<string>(type: "text", nullable: true),
                    ConsignorSettings = table.Column<string>(type: "text", nullable: true),
                    NotificationSettings = table.Column<string>(type: "text", nullable: true),
                    ReceiptSettings = table.Column<string>(type: "text", nullable: true),
                    BrandingSettings = table.Column<string>(type: "text", nullable: true),
                    StripeCustomerId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    StripeSubscriptionId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    IsFounderPricing = table.Column<bool>(type: "boolean", nullable: false),
                    FounderTier = table.Column<int>(type: "integer", nullable: true),
                    CachedSubscriptionStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CachedPeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ActiveIntegrations = table.Column<string[]>(type: "text[]", nullable: true),
                    QuickBooksConnected = table.Column<bool>(type: "boolean", nullable: false),
                    QuickBooksRealmId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    QuickBooksAccessToken = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    QuickBooksRefreshToken = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    QuickBooksTokenExpiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    QuickBooksLastSync = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StoreCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    StoreCodeEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AutoApproveConsignors = table.Column<bool>(type: "boolean", nullable: false),
                    ApprovalMode = table.Column<int>(type: "integer", nullable: false),
                    AgreementMethod = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AcknowledgeTermsText = table.Column<string>(type: "text", nullable: true),
                    AgreementTemplateCloudinaryUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SetupCompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SetupStep = table.Column<int>(type: "integer", nullable: false),
                    OnboardingDismissed = table.Column<bool>(type: "boolean", nullable: false),
                    WelcomeGuideCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    SetupChecklistDismissed = table.Column<bool>(type: "boolean", nullable: false),
                    ShopName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ShopDescription = table.Column<string>(type: "text", nullable: true),
                    ShopLogoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ShopBannerUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ShopAddress1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ShopAddress2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ShopCity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ShopState = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ShopZip = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ShopCountry = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ShopPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ShopEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ShopWebsite = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ShopTimezone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DefaultSplitPercentage = table.Column<decimal>(type: "numeric", nullable: false),
                    TaxRate = table.Column<decimal>(type: "numeric", nullable: false),
                    ZipCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    StoreEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ShippingEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ShippingFlatRate = table.Column<decimal>(type: "numeric", nullable: false),
                    PickupEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    PickupInstructions = table.Column<string>(type: "text", nullable: true),
                    PayOnPickupEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    OnlinePaymentEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    OwnerPin = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RequirePinForTaxExempt = table.Column<bool>(type: "boolean", nullable: false),
                    RequirePinForReturnsOver = table.Column<decimal>(type: "numeric", nullable: false),
                    RequirePinForVoid = table.Column<bool>(type: "boolean", nullable: false),
                    RequirePinForDrawerOpen = table.Column<bool>(type: "boolean", nullable: false),
                    QuickBooksCompanyId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    StripeConnected = table.Column<bool>(type: "boolean", nullable: false),
                    SendGridConnected = table.Column<bool>(type: "boolean", nullable: false),
                    CloudinaryConnected = table.Column<bool>(type: "boolean", nullable: false),
                    IntegrationMode = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BookkeepingSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UseQuickBooks = table.Column<bool>(type: "boolean", nullable: false),
                    QuickBooksConnected = table.Column<bool>(type: "boolean", nullable: false),
                    QuickBooksCompanyId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    QuickBooksCompanyName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    QuickBooksLastSync = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SalesSyncEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ConsignorSyncEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    PayoutRecordingMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LineItemDetail = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SyncFrequency = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AccountMappings = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    AutoCreateVendors = table.Column<bool>(type: "boolean", nullable: false),
                    SyncItemsAsProducts = table.Column<bool>(type: "boolean", nullable: false),
                    TrackInventoryQuantities = table.Column<bool>(type: "boolean", nullable: false),
                    ContinueOnSyncErrors = table.Column<bool>(type: "boolean", nullable: false),
                    LastSyncAttempt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSuccessfulSync = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSyncError = table.Column<string>(type: "text", nullable: true),
                    EnableCsvExport = table.Column<bool>(type: "boolean", nullable: false),
                    EnableExcelExport = table.Column<bool>(type: "boolean", nullable: false),
                    ExportFilePrefix = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookkeepingSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookkeepingSettings_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClerkPermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShowConsignorNames = table.Column<bool>(type: "boolean", nullable: false),
                    ShowItemCost = table.Column<bool>(type: "boolean", nullable: false),
                    AllowReturns = table.Column<bool>(type: "boolean", nullable: false),
                    MaxReturnAmountWithoutPin = table.Column<decimal>(type: "numeric", nullable: false),
                    AllowDiscounts = table.Column<bool>(type: "boolean", nullable: false),
                    MaxDiscountPercentWithoutPin = table.Column<int>(type: "integer", nullable: false),
                    AllowVoid = table.Column<bool>(type: "boolean", nullable: false),
                    AllowDrawerOpen = table.Column<bool>(type: "boolean", nullable: false),
                    AllowEndOfDayCount = table.Column<bool>(type: "boolean", nullable: false),
                    AllowPriceOverride = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClerkPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClerkPermissions_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FirstName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LastName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    StripeCustomerId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsEmailVerified = table.Column<bool>(type: "boolean", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PasswordHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailVerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EmailVerificationToken = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    PasswordResetToken = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    PasswordResetTokenExpiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Customers_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FileUploadHashes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FileType = table.Column<string>(type: "text", nullable: false),
                    FirstRowSample = table.Column<string>(type: "text", nullable: true),
                    RowCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileUploadHashes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileUploadHashes_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "IntegrationCredentials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    IntegrationType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CredentialsEncrypted = table.Column<string>(type: "text", nullable: false),
                    AccessTokenExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RefreshTokenExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastErrorAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationCredentials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegrationCredentials_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Color = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    SquareCategoryId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ParentCategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    DefaultCommissionRate = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemCategories_ItemCategories_ParentCategoryId",
                        column: x => x.ParentCategoryId,
                        principalTable: "ItemCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ItemCategories_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemTags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Color = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemTags_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateTable(
                name: "PayoutSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayoutMethodCheck = table.Column<bool>(type: "boolean", nullable: false),
                    PayoutMethodCash = table.Column<bool>(type: "boolean", nullable: false),
                    PayoutMethodACH = table.Column<bool>(type: "boolean", nullable: false),
                    PayoutMethodVenmo = table.Column<bool>(type: "boolean", nullable: false),
                    PayoutMethodPayPal = table.Column<bool>(type: "boolean", nullable: false),
                    PayoutMethodStoreCredit = table.Column<bool>(type: "boolean", nullable: false),
                    PayoutMethodZelle = table.Column<bool>(type: "boolean", nullable: false),
                    HoldPeriodDays = table.Column<int>(type: "integer", nullable: false),
                    MinimumPayoutThreshold = table.Column<decimal>(type: "numeric", nullable: false),
                    MinimumBalanceProtection = table.Column<decimal>(type: "numeric", nullable: false),
                    BankAccountConnected = table.Column<bool>(type: "boolean", nullable: false),
                    PlaidAccessToken = table.Column<string>(type: "text", nullable: false),
                    PlaidAccountId = table.Column<string>(type: "text", nullable: false),
                    BankName = table.Column<string>(type: "text", nullable: false),
                    BankAccountLast4 = table.Column<string>(type: "text", nullable: false),
                    DwollaCustomerId = table.Column<string>(type: "text", nullable: false),
                    DwollaCustomerUrl = table.Column<string>(type: "text", nullable: false),
                    DwollaCustomerStatus = table.Column<string>(type: "text", nullable: false),
                    AutoPayEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AutoPayMonday = table.Column<bool>(type: "boolean", nullable: false),
                    AutoPayTuesday = table.Column<bool>(type: "boolean", nullable: false),
                    AutoPayWednesday = table.Column<bool>(type: "boolean", nullable: false),
                    AutoPayThursday = table.Column<bool>(type: "boolean", nullable: false),
                    AutoPayFriday = table.Column<bool>(type: "boolean", nullable: false),
                    AutoPaySaturday = table.Column<bool>(type: "boolean", nullable: false),
                    AutoPaySunday = table.Column<bool>(type: "boolean", nullable: false),
                    ExtendedSettings = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayoutSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayoutSettings_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PendingSquareImports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SquareCatalogId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SquareVariationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Price = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Sku = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Barcode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SquareUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ImportedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingSquareImports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PendingSquareImports_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PendingSquareTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SquarePaymentId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SquareLocationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SquareCreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PaymentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SquarePaymentMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SquareCardBrand = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Subtotal = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    TaxRate = table.Column<decimal>(type: "numeric(5,4)", nullable: false),
                    Total = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    CustomerEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    TaxCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingSquareTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PendingSquareTransactions_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QBSyncLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShopId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    QBId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QBSyncLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QBSyncLogs_Organizations_ShopId",
                        column: x => x.ShopId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SquareConnections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsConnected = table.Column<bool>(type: "boolean", nullable: false),
                    MerchantId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AccessToken = table.Column<string>(type: "text", nullable: true),
                    RefreshToken = table.Column<string>(type: "text", nullable: true),
                    TokenExpiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LocationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LocationName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    LastSyncAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AutoSync = table.Column<bool>(type: "boolean", nullable: false),
                    SyncSchedule = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SquareConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SquareConnections_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StripeEventId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RawJson = table.Column<string>(type: "text", nullable: false),
                    Processed = table.Column<bool>(type: "boolean", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubscriptionEvents_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                    ApprovalStatus = table.Column<int>(type: "integer", nullable: false),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectedReason = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PreferredName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ClerkPin = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    HiredDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CustomerPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    FulfillmentType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ShippingAddress1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ShippingAddress2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ShippingCity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ShippingState = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ShippingZip = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ShippingCountry = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    ShippingAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    PaymentMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PaymentStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PaymentIntentId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    ShippedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TrackingNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Orders_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Orders_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShoppingCarts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShoppingCarts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShoppingCarts_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ShoppingCarts_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SquareSyncLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SquareConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SyncStarted = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SyncCompleted = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    TransactionsImported = table.Column<int>(type: "integer", nullable: false),
                    TransactionsMatched = table.Column<int>(type: "integer", nullable: false),
                    TransactionsUnmatched = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    Details = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SquareSyncLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SquareSyncLogs_SquareConnections_SquareConnectionId",
                        column: x => x.SquareConnectionId,
                        principalTable: "SquareConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OldValues = table.Column<string>(type: "text", nullable: true),
                    NewValues = table.Column<string>(type: "text", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SessionId = table.Column<string>(type: "text", nullable: true),
                    CorrelationId = table.Column<string>(type: "text", nullable: true),
                    RiskLevel = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

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
                name: "Consignors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ConsignorNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PreferredName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    AddressLine1 = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    AddressLine2 = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    State = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CommissionRate = table.Column<decimal>(type: "numeric(5,4)", nullable: false),
                    ContractStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ContractEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PreferredPaymentMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PaymentDetails = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Status = table.Column<int>(type: "integer", maxLength: 20, nullable: false),
                    StatusChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StatusChangedReason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ApprovalStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    RejectedReason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    IsShopOwned = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    BusinessName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Address = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ZipCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    PaymentMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DefaultSplitPercentage = table.Column<decimal>(type: "numeric(5,4)", nullable: true),
                    PortalAccess = table.Column<bool>(type: "boolean", nullable: false),
                    InviteCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    InviteExpiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AgreementGeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AgreementGeneratedUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AcknowledgedTermsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AcknowledgedTermsIP = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    SignedAgreementUploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SignedAgreementUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AgreementVerificationResult = table.Column<string>(type: "jsonb", nullable: true),
                    AgreementStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    AgreementReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AgreementReviewedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    AgreementReviewNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    QBVendorId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Consignors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Consignors_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Consignors_Users_ApprovedBy",
                        column: x => x.ApprovedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Consignors_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Consignors_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Consignors_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "NotificationPreferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SmsOptedIn = table.Column<bool>(type: "boolean", nullable: false),
                    SmsPhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    SmsPhoneVerified = table.Column<bool>(type: "boolean", nullable: false),
                    SmsPhoneVerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TypePreferences = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    EmailItemSold = table.Column<bool>(type: "boolean", nullable: false),
                    EmailPayoutProcessed = table.Column<bool>(type: "boolean", nullable: false),
                    EmailPayoutPending = table.Column<bool>(type: "boolean", nullable: false),
                    EmailItemExpired = table.Column<bool>(type: "boolean", nullable: false),
                    EmailStatementReady = table.Column<bool>(type: "boolean", nullable: false),
                    EmailAccountUpdate = table.Column<bool>(type: "boolean", nullable: false),
                    DigestMode = table.Column<string>(type: "text", nullable: false),
                    DigestTime = table.Column<string>(type: "text", nullable: false),
                    DigestDay = table.Column<int>(type: "integer", nullable: false),
                    PayoutPendingThreshold = table.Column<decimal>(type: "numeric", nullable: false),
                    EmailEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationPreferences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateTable(
                name: "Shoppers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PreferredName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ShippingAddress1 = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ShippingAddress2 = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ShippingCity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ShippingState = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ShippingZip = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    EmailNotifications = table.Column<bool>(type: "boolean", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "UserRoleAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AssignedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    RoleData = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoleAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRoleAssignments_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoleAssignments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConsignorAgreements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsignorId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Method = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    DocumentUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DocumentStorageKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MarkedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsignorAgreements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsignorAgreements_Consignors_ConsignorId",
                        column: x => x.ConsignorId,
                        principalTable: "Consignors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConsignorAgreements_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConsignorAgreements_Users_MarkedByUserId",
                        column: x => x.MarkedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ConsignorAgreementUploads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsignorId = table.Column<Guid>(type: "uuid", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FileUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FileMimeType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FileSizeBytes = table.Column<int>(type: "integer", nullable: true),
                    VerificationResult = table.Column<string>(type: "jsonb", nullable: true),
                    VerificationProvider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Recommendation = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewDecision = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ReviewNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsignorAgreementUploads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsignorAgreementUploads_Consignors_ConsignorId",
                        column: x => x.ConsignorId,
                        principalTable: "Consignors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConsignorAgreementUploads_Users_ReviewedBy",
                        column: x => x.ReviewedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DropoffRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsignorId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlannedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    PlannedTimeSlot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ConsignorMessage = table.Column<string>(type: "text", nullable: true),
                    OwnerNotes = table.Column<string>(type: "text", nullable: true),
                    ItemsJson = table.Column<string>(type: "jsonb", nullable: false),
                    ItemCount = table.Column<int>(type: "integer", nullable: false),
                    SuggestedTotal = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ImportedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelReason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectionReason = table.Column<string>(type: "text", nullable: true),
                    ReopenedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PhotosPurgedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MinimumTotal = table.Column<decimal>(type: "numeric", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DropoffRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DropoffRequests_Consignors_ConsignorId",
                        column: x => x.ConsignorId,
                        principalTable: "Consignors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DropoffRequests_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FundingSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsignorId = table.Column<Guid>(type: "uuid", nullable: true),
                    DwollaFundingSourceId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    DwollaFundingSourceUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PlaidAccessToken = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    PlaidAccountId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    BankName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    AccountType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AccountNumberMask = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RemovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundingSources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FundingSources_Consignors_ConsignorId",
                        column: x => x.ConsignorId,
                        principalTable: "Consignors",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FundingSources_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Payouts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsignorId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayoutNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PayoutDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PaymentMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PaymentReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BatchId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    BatchCreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TransactionCount = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    SyncedToQuickBooks = table.Column<bool>(type: "boolean", nullable: false),
                    QuickBooksBillId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    QBBillId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    QBPaymentId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    QBSyncStatus = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payouts_Consignors_ConsignorId",
                        column: x => x.ConsignorId,
                        principalTable: "Consignors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payouts_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PayoutSummaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsignorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClearedAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    UnclearedAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalPendingAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    ClearedTransactionCount = table.Column<int>(type: "integer", nullable: false),
                    UnclearedTransactionCount = table.Column<int>(type: "integer", nullable: false),
                    TotalTransactionCount = table.Column<int>(type: "integer", nullable: false),
                    EarliestSaleDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LatestSaleDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextClearDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MeetsMinimumThreshold = table.Column<bool>(type: "boolean", nullable: false),
                    LowestMinimumThreshold = table.Column<decimal>(type: "numeric", nullable: false),
                    NotificationSent = table.Column<bool>(type: "boolean", nullable: false),
                    NotificationSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastComputedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayoutSummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayoutSummaries_Consignors_ConsignorId",
                        column: x => x.ConsignorId,
                        principalTable: "Consignors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PayoutSummaries_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateTable(
                name: "Statements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsignorId = table.Column<Guid>(type: "uuid", nullable: false),
                    StatementNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    OpeningBalance = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalSales = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalEarnings = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalPayouts = table.Column<decimal>(type: "numeric", nullable: false),
                    ClosingBalance = table.Column<decimal>(type: "numeric", nullable: false),
                    ItemsSold = table.Column<int>(type: "integer", nullable: false),
                    ItemsAdded = table.Column<int>(type: "integer", nullable: false),
                    ItemsRemoved = table.Column<int>(type: "integer", nullable: false),
                    PayoutCount = table.Column<int>(type: "integer", nullable: false),
                    PdfUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ViewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Statements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Statements_Consignors_ConsignorId",
                        column: x => x.ConsignorId,
                        principalTable: "Consignors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Statements_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AchTransfers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayoutId = table.Column<Guid>(type: "uuid", nullable: true),
                    ConsignorId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceFundingSourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    DestinationFundingSourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    DwollaTransferId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    DwollaTransferUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DwollaStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FailureReason = table.Column<string>(type: "text", nullable: true),
                    InitiatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchTransfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AchTransfers_Consignors_ConsignorId",
                        column: x => x.ConsignorId,
                        principalTable: "Consignors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AchTransfers_FundingSources_DestinationFundingSourceId",
                        column: x => x.DestinationFundingSourceId,
                        principalTable: "FundingSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AchTransfers_FundingSources_SourceFundingSourceId",
                        column: x => x.SourceFundingSourceId,
                        principalTable: "FundingSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AchTransfers_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AchTransfers_Payouts_PayoutId",
                        column: x => x.PayoutId,
                        principalTable: "Payouts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    TransactionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SaleDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PaymentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    TaxRate = table.Column<decimal>(type: "numeric(5,4)", nullable: false),
                    Total = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    CustomerEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    SalesTaxAmount = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    TaxCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SquarePaymentId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SquareLocationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ImportedFromSquare = table.Column<bool>(type: "boolean", nullable: false),
                    SquareCreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SyncedToQuickBooks = table.Column<bool>(type: "boolean", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    QuickBooksSalesReceiptId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    QBSalesReceiptId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    QuickBooksSyncFailed = table.Column<bool>(type: "boolean", nullable: false),
                    QuickBooksSyncError = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    ClearDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConsignorPaidOut = table.Column<bool>(type: "boolean", nullable: false),
                    ConsignorPaidOutDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PayoutMethod = table.Column<string>(type: "text", nullable: true),
                    PayoutNotes = table.Column<string>(type: "text", nullable: true),
                    PayoutId = table.Column<Guid>(type: "uuid", nullable: true),
                    PayoutStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ProcessedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProcessedByName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ConsignorId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transactions_Consignors_ConsignorId",
                        column: x => x.ConsignorId,
                        principalTable: "Consignors",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Transactions_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Transactions_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Transactions_Payouts_PayoutId",
                        column: x => x.PayoutId,
                        principalTable: "Payouts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Transactions_Users_ProcessedByUserId",
                        column: x => x.ProcessedByUserId,
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
                    ConsignorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sku = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Barcode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ItemCategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    Brand = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Size = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Color = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Condition = table.Column<int>(type: "integer", nullable: false),
                    Materials = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Measurements = table.Column<string>(type: "text", nullable: true),
                    Price = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    OriginalPrice = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    MinimumPrice = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StatusChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StatusChangedReason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ReceivedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ListedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ExpirationDate = table.Column<DateOnly>(type: "date", nullable: true),
                    SoldDate = table.Column<DateOnly>(type: "date", nullable: true),
                    PrimaryImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Location = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    InternalNotes = table.Column<string>(type: "text", nullable: true),
                    SquareCatalogId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SquareVariationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SquareLastSyncAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsSquareManaged = table.Column<bool>(type: "boolean", nullable: false),
                    Photos = table.Column<string>(type: "text", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    TransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Items_Consignors_ConsignorId",
                        column: x => x.ConsignorId,
                        principalTable: "Consignors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Items_ItemCategories_ItemCategoryId",
                        column: x => x.ItemCategoryId,
                        principalTable: "ItemCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Items_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Items_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Items_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Items_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CartItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CartId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CartItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CartItems_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CartItems_ShoppingCarts_CartId",
                        column: x => x.CartId,
                        principalTable: "ShoppingCarts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerWishlist",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerWishlist", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerWishlist_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerWishlist_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemImages_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemImages_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ItemRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsignorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Brand = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Size = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Color = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Condition = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Measurements = table.Column<string>(type: "text", nullable: true),
                    SuggestedPrice = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    MinAcceptablePrice = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    OriginalPurchasePrice = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    ConsignorNotes = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ReviewedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectionReason = table.Column<string>(type: "text", nullable: true),
                    OwnerNotes = table.Column<string>(type: "text", nullable: true),
                    ApprovedItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedPrice = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    ApprovedCategory = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    OriginalRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResubmissionCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemRequests_Consignors_ConsignorId",
                        column: x => x.ConsignorId,
                        principalTable: "Consignors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ItemRequests_ItemRequests_OriginalRequestId",
                        column: x => x.OriginalRequestId,
                        principalTable: "ItemRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ItemRequests_Items_ApprovedItemId",
                        column: x => x.ApprovedItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ItemRequests_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemRequests_Users_ReviewedBy",
                        column: x => x.ReviewedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ItemTagAssignments",
                columns: table => new
                {
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemTagId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemTagAssignments", x => new { x.ItemId, x.ItemTagId });
                    table.ForeignKey(
                        name: "FK_ItemTagAssignments_ItemTags_ItemTagId",
                        column: x => x.ItemTagId,
                        principalTable: "ItemTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemTagAssignments_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsignorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ItemPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    SplitPercentage = table.Column<decimal>(type: "numeric", nullable: false),
                    CommissionAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItems_Consignors_ConsignorId",
                        column: x => x.ConsignorId,
                        principalTable: "Consignors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderItems_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderItems_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PendingImportItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Price = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    MinimumPrice = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    Sku = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Brand = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Condition = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    SourceReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ImagePublicId = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ConsignorId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    IsSelectedForAssignment = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ImportedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ImportedItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingImportItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PendingImportItems_Consignors_ConsignorId",
                        column: x => x.ConsignorId,
                        principalTable: "Consignors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PendingImportItems_Items_ImportedItemId",
                        column: x => x.ImportedItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PendingImportItems_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PendingSquareTransactionItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PendingTransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PendingSquareImportId = table.Column<Guid>(type: "uuid", nullable: true),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    SquareCatalogId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SquareVariationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    ItemName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ItemDescription = table.Column<string>(type: "text", nullable: true),
                    ItemSku = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ConsignorSplitPercentage = table.Column<decimal>(type: "numeric(5,4)", nullable: true),
                    ConsignorAmount = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    StoreAmount = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    ConsignorId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingSquareTransactionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PendingSquareTransactionItems_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PendingSquareTransactionItems_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PendingSquareTransactionItems_PendingSquareImports_PendingS~",
                        column: x => x.PendingSquareImportId,
                        principalTable: "PendingSquareImports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PendingSquareTransactionItems_PendingSquareTransactions_Pen~",
                        column: x => x.PendingTransactionId,
                        principalTable: "PendingSquareTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TransactionItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsignorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    ConsignorSplitPercentage = table.Column<decimal>(type: "numeric(5,4)", nullable: false),
                    ConsignorAmount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    StoreAmount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransactionItems_Consignors_ConsignorId",
                        column: x => x.ConsignorId,
                        principalTable: "Consignors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransactionItems_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransactionItems_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TransactionItems_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemRequestImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ThumbnailUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemRequestImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemRequestImages_ItemRequests_ItemRequestId",
                        column: x => x.ItemRequestId,
                        principalTable: "ItemRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    FromUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    FromType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ToUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ActionStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ActionCompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ActionCompletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: true),
                    AttachmentUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AttachmentName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AttachmentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ActionUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    TransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    PayoutId = table.Column<Guid>(type: "uuid", nullable: true),
                    StatementId = table.Column<Guid>(type: "uuid", nullable: true),
                    ItemRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReferenceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ReferenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Metadata = table.Column<string>(type: "text", nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsImportant = table.Column<bool>(type: "boolean", nullable: false),
                    MarkedImportantAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MarkedImportantByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsAcknowledged = table.Column<bool>(type: "boolean", nullable: false),
                    AcknowledgedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AcknowledgedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    EmailSent = table.Column<bool>(type: "boolean", nullable: false),
                    EmailSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SmsSent = table.Column<bool>(type: "boolean", nullable: false),
                    SmsSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_ItemRequests_ItemRequestId",
                        column: x => x.ItemRequestId,
                        principalTable: "ItemRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Notifications_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Notifications_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Notifications_Payouts_PayoutId",
                        column: x => x.PayoutId,
                        principalTable: "Payouts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Notifications_Statements_StatementId",
                        column: x => x.StatementId,
                        principalTable: "Statements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Notifications_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_AcknowledgedByUserId",
                        column: x => x.AcknowledgedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_ActionCompletedByUserId",
                        column: x => x.ActionCompletedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_FromUserId",
                        column: x => x.FromUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_MarkedImportantByUserId",
                        column: x => x.MarkedImportantByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Notifications_Users_ToUserId",
                        column: x => x.ToUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Organizations",
                columns: new[] { "Id", "AcknowledgeTermsText", "ActiveIntegrations", "AgreementMethod", "AgreementTemplateCloudinaryUrl", "ApprovalMode", "AutoApproveConsignors", "BrandingSettings", "BusinessSettings", "CachedPeriodEnd", "CachedSubscriptionStatus", "CloudinaryConnected", "ConsignorPermissions", "ConsignorSettings", "CreatedAt", "Currency", "DefaultSplitPercentage", "FounderTier", "IntegrationMode", "IsFounderPricing", "Name", "NotificationSettings", "OnboardingDismissed", "OnlinePaymentEnabled", "OwnerPin", "PayOnPickupEnabled", "PickupEnabled", "PickupInstructions", "QuickBooksAccessToken", "QuickBooksCompanyId", "QuickBooksConnected", "QuickBooksLastSync", "QuickBooksRealmId", "QuickBooksRefreshToken", "QuickBooksTokenExpiry", "ReceiptSettings", "RequirePinForDrawerOpen", "RequirePinForReturnsOver", "RequirePinForTaxExempt", "RequirePinForVoid", "SalesSettings", "SendGridConnected", "Settings", "SetupChecklistDismissed", "SetupCompletedAt", "SetupStep", "ShippingEnabled", "ShippingFlatRate", "ShopAddress1", "ShopAddress2", "ShopBannerUrl", "ShopCity", "ShopCountry", "ShopDescription", "ShopEmail", "ShopLogoUrl", "ShopName", "ShopPhone", "ShopState", "ShopTimezone", "ShopWebsite", "ShopZip", "Slug", "StoreCode", "StoreCodeEnabled", "StoreEnabled", "StorefrontSettings", "StripeConnected", "StripeCustomerId", "StripeSubscriptionId", "Subdomain", "TaxRate", "UpdatedAt", "VerticalType", "WelcomeGuideCompleted", "ZipCode" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), null, null, "none", null, 1, true, null, null, null, "active", false, null, null, new DateTime(2026, 2, 21, 20, 28, 7, 828, DateTimeKind.Utc).AddTicks(5734), "USD", 60.00m, 1, 0, true, "Demo Consignment Shop", null, false, false, null, true, true, null, null, null, false, null, null, null, null, null, true, 100.00m, true, true, null, false, null, false, null, 0, false, 0m, null, null, null, null, "US", null, null, null, null, null, null, "America/New_York", null, null, null, null, true, false, null, false, null, null, "demo-shop", 0.0000m, new DateTime(2026, 2, 21, 20, 28, 7, 828, DateTimeKind.Utc).AddTicks(5734), 1, false, null });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "ApprovalStatus", "ApprovedAt", "ApprovedBy", "ClerkPin", "CreatedAt", "Email", "HiredDate", "IsActive", "LastLoginAt", "Name", "OrganizationId", "PasswordHash", "Phone", "PreferredName", "RejectedReason", "Role", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("22222222-2222-2222-2222-222222222222"), 1, null, null, null, new DateTime(2026, 2, 21, 20, 28, 8, 374, DateTimeKind.Utc).AddTicks(433), "admin@microsaasbuilders.com", null, true, null, null, new Guid("11111111-1111-1111-1111-111111111111"), "$2a$11$RQyy6RL9J489/Isgx7elFu.hHhUy7ExCfNQFJHyofvK/GbiucOVw6", null, null, null, 1, new DateTime(2026, 2, 21, 20, 28, 8, 374, DateTimeKind.Utc).AddTicks(465) },
                    { new Guid("33333333-3333-3333-3333-333333333333"), 1, null, null, null, new DateTime(2026, 2, 21, 20, 28, 8, 374, DateTimeKind.Utc).AddTicks(511), "owner1@microsaasbuilders.com", null, true, null, null, new Guid("11111111-1111-1111-1111-111111111111"), "$2a$11$RQyy6RL9J489/Isgx7elFu.hHhUy7ExCfNQFJHyofvK/GbiucOVw6", null, null, null, 1, new DateTime(2026, 2, 21, 20, 28, 8, 374, DateTimeKind.Utc).AddTicks(512) },
                    { new Guid("44444444-4444-4444-4444-444444444444"), 1, null, null, null, new DateTime(2026, 2, 21, 20, 28, 8, 374, DateTimeKind.Utc).AddTicks(542), "consignor1@microsaasbuilders.com", null, true, null, null, new Guid("11111111-1111-1111-1111-111111111111"), "$2a$11$RQyy6RL9J489/Isgx7elFu.hHhUy7ExCfNQFJHyofvK/GbiucOVw6", null, null, null, 2, new DateTime(2026, 2, 21, 20, 28, 8, 374, DateTimeKind.Utc).AddTicks(542) },
                    { new Guid("55555555-5555-5555-5555-555555555555"), 1, null, null, null, new DateTime(2026, 2, 21, 20, 28, 8, 374, DateTimeKind.Utc).AddTicks(558), "customer1@microsaasbuilders.com", null, true, null, null, new Guid("11111111-1111-1111-1111-111111111111"), "$2a$11$RQyy6RL9J489/Isgx7elFu.hHhUy7ExCfNQFJHyofvK/GbiucOVw6", null, null, null, 3, new DateTime(2026, 2, 21, 20, 28, 8, 374, DateTimeKind.Utc).AddTicks(559) }
                });

            migrationBuilder.InsertData(
                table: "Consignors",
                columns: new[] { "Id", "AcknowledgedTermsAt", "AcknowledgedTermsIP", "Address", "AddressLine1", "AddressLine2", "AgreementGeneratedAt", "AgreementGeneratedUrl", "AgreementReviewNote", "AgreementReviewedAt", "AgreementReviewedBy", "AgreementStatus", "AgreementVerificationResult", "ApprovalStatus", "ApprovedAt", "ApprovedBy", "BusinessName", "City", "CommissionRate", "ConsignorNumber", "ContractEndDate", "ContractStartDate", "CreatedAt", "CreatedBy", "DefaultSplitPercentage", "DisplayName", "Email", "InviteCode", "InviteExpiry", "IsShopOwned", "Name", "Notes", "OrganizationId", "PaymentDetails", "PaymentMethod", "Phone", "PortalAccess", "PostalCode", "PreferredName", "PreferredPaymentMethod", "QBVendorId", "RejectedReason", "SignedAgreementUploadedAt", "SignedAgreementUrl", "State", "Status", "StatusChangedAt", "StatusChangedReason", "UpdatedAt", "UpdatedBy", "UserId", "ZipCode" },
                values: new object[] { new Guid("66666666-6666-6666-6666-666666666666"), null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, 0.6000m, "PRV-00001", null, null, new DateTime(2026, 2, 21, 20, 28, 8, 374, DateTimeKind.Utc).AddTicks(752), null, null, null, "consignor1@microsaasbuilders.com", null, null, false, "Demo Artist", null, new Guid("11111111-1111-1111-1111-111111111111"), null, null, "(555) 123-4567", false, null, "Demo", null, null, null, null, null, null, 1, null, null, new DateTime(2026, 2, 21, 20, 28, 8, 374, DateTimeKind.Utc).AddTicks(753), null, new Guid("44444444-4444-4444-4444-444444444444"), null });

            migrationBuilder.CreateIndex(
                name: "IX_AchTransfers_ConsignorId",
                table: "AchTransfers",
                column: "ConsignorId");

            migrationBuilder.CreateIndex(
                name: "IX_AchTransfers_DestinationFundingSourceId",
                table: "AchTransfers",
                column: "DestinationFundingSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_AchTransfers_OrganizationId",
                table: "AchTransfers",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_AchTransfers_PayoutId",
                table: "AchTransfers",
                column: "PayoutId");

            migrationBuilder.CreateIndex(
                name: "IX_AchTransfers_SourceFundingSourceId",
                table: "AchTransfers",
                column: "SourceFundingSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CreatedAt",
                table: "AuditLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityType_EntityId",
                table: "AuditLogs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_OrganizationId",
                table: "AuditLogs",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BookkeepingSettings_OrganizationId",
                table: "BookkeepingSettings",
                column: "OrganizationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_CartId",
                table: "CartItems",
                column: "CartId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_CartId_ItemId",
                table: "CartItems",
                columns: new[] { "CartId", "ItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_ItemId",
                table: "CartItems",
                column: "ItemId");

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

            migrationBuilder.CreateIndex(
                name: "IX_ClerkPermissions_OrganizationId",
                table: "ClerkPermissions",
                column: "OrganizationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConsignorAgreements_ConsignorId",
                table: "ConsignorAgreements",
                column: "ConsignorId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConsignorAgreements_MarkedByUserId",
                table: "ConsignorAgreements",
                column: "MarkedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsignorAgreements_OrganizationId",
                table: "ConsignorAgreements",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsignorAgreementUploads_ConsignorId",
                table: "ConsignorAgreementUploads",
                column: "ConsignorId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsignorAgreementUploads_ReviewDecision",
                table: "ConsignorAgreementUploads",
                column: "ReviewDecision");

            migrationBuilder.CreateIndex(
                name: "IX_ConsignorAgreementUploads_ReviewedAt",
                table: "ConsignorAgreementUploads",
                column: "ReviewedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ConsignorAgreementUploads_ReviewedBy",
                table: "ConsignorAgreementUploads",
                column: "ReviewedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ConsignorAgreementUploads_UploadedAt",
                table: "ConsignorAgreementUploads",
                column: "UploadedAt");

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
                name: "IX_Consignors_ApprovalStatus",
                table: "Consignors",
                column: "ApprovalStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Consignors_ApprovedBy",
                table: "Consignors",
                column: "ApprovedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Consignors_CreatedBy",
                table: "Consignors",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Consignors_OrganizationId_ConsignorNumber",
                table: "Consignors",
                columns: new[] { "OrganizationId", "ConsignorNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Consignors_OrganizationId_Email",
                table: "Consignors",
                columns: new[] { "OrganizationId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Consignors_OrganizationId_Status",
                table: "Consignors",
                columns: new[] { "OrganizationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Consignors_UpdatedBy",
                table: "Consignors",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Consignors_UserId",
                table: "Consignors",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_OrganizationId",
                table: "Customers",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_OrganizationId_Email",
                table: "Customers",
                columns: new[] { "OrganizationId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerWishlist_CustomerId",
                table: "CustomerWishlist",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerWishlist_ItemId",
                table: "CustomerWishlist",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_DropoffRequests_ConsignorId",
                table: "DropoffRequests",
                column: "ConsignorId");

            migrationBuilder.CreateIndex(
                name: "IX_DropoffRequests_CreatedAt",
                table: "DropoffRequests",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DropoffRequests_OrganizationId",
                table: "DropoffRequests",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_DropoffRequests_PlannedDate",
                table: "DropoffRequests",
                column: "PlannedDate");

            migrationBuilder.CreateIndex(
                name: "IX_DropoffRequests_Status",
                table: "DropoffRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_FileUploadHashes_OrganizationId",
                table: "FileUploadHashes",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_FundingSources_ConsignorId",
                table: "FundingSources",
                column: "ConsignorId");

            migrationBuilder.CreateIndex(
                name: "IX_FundingSources_OrganizationId",
                table: "FundingSources",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_GuestCheckouts_ExpiresAt",
                table: "GuestCheckouts",
                column: "ExpiresAt");

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
                name: "IX_IntegrationCredentials_OrganizationId",
                table: "IntegrationCredentials",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationCredentials_OrganizationId_IntegrationType",
                table: "IntegrationCredentials",
                columns: new[] { "OrganizationId", "IntegrationType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemCategories_OrganizationId",
                table: "ItemCategories",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemCategories_OrganizationId_Name",
                table: "ItemCategories",
                columns: new[] { "OrganizationId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemCategories_ParentCategoryId",
                table: "ItemCategories",
                column: "ParentCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemImages_CreatedBy",
                table: "ItemImages",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ItemImages_ItemId",
                table: "ItemImages",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemRequestImages_ItemRequestId",
                table: "ItemRequestImages",
                column: "ItemRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemRequests_ApprovedItemId",
                table: "ItemRequests",
                column: "ApprovedItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemRequests_ConsignorId",
                table: "ItemRequests",
                column: "ConsignorId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemRequests_CreatedAt",
                table: "ItemRequests",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ItemRequests_OrganizationId",
                table: "ItemRequests",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemRequests_OriginalRequestId",
                table: "ItemRequests",
                column: "OriginalRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemRequests_ReviewedBy",
                table: "ItemRequests",
                column: "ReviewedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ItemRequests_Status",
                table: "ItemRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Items_ConsignorId",
                table: "Items",
                column: "ConsignorId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_CreatedBy",
                table: "Items",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Items_ItemCategoryId",
                table: "Items",
                column: "ItemCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_OrganizationId_ItemCategoryId",
                table: "Items",
                columns: new[] { "OrganizationId", "ItemCategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_Items_OrganizationId_Sku",
                table: "Items",
                columns: new[] { "OrganizationId", "Sku" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Items_OrganizationId_Status",
                table: "Items",
                columns: new[] { "OrganizationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Items_TransactionId",
                table: "Items",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_UpdatedBy",
                table: "Items",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ItemTagAssignments_ItemTagId",
                table: "ItemTagAssignments",
                column: "ItemTagId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemTags_OrganizationId",
                table: "ItemTags",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemTags_OrganizationId_Name",
                table: "ItemTags",
                columns: new[] { "OrganizationId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_NotificationPreferences_User_Role",
                table: "NotificationPreferences",
                columns: new[] { "UserId", "Role" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_AcknowledgedByUserId",
                table: "Notifications",
                column: "AcknowledgedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ActionCompletedByUserId",
                table: "Notifications",
                column: "ActionCompletedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_FromUserId",
                table: "Notifications",
                column: "FromUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ItemId",
                table: "Notifications",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ItemRequestId",
                table: "Notifications",
                column: "ItemRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_MarkedImportantByUserId",
                table: "Notifications",
                column: "MarkedImportantByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Org",
                table: "Notifications",
                columns: new[] { "OrganizationId", "ToType", "CreatedAt" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_PayoutId",
                table: "Notifications",
                column: "PayoutId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_StatementId",
                table: "Notifications",
                column: "StatementId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_To_User_Type",
                table: "Notifications",
                columns: new[] { "ToUserId", "ToType", "IsRead", "DeletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ToUserId",
                table: "Notifications",
                column: "ToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_TransactionId",
                table: "Notifications",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Type",
                table: "Notifications",
                columns: new[] { "Type", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_ConsignorId",
                table: "OrderItems",
                column: "ConsignorId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_ItemId",
                table: "OrderItems",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId",
                table: "OrderItems",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId_ItemId",
                table: "OrderItems",
                columns: new[] { "OrderId", "ItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CreatedAt",
                table: "Orders",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerId",
                table: "Orders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderNumber",
                table: "Orders",
                column: "OrderNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrganizationId",
                table: "Orders",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrganizationId_OrderNumber",
                table: "Orders",
                columns: new[] { "OrganizationId", "OrderNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status",
                table: "Orders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_CachedSubscriptionStatus",
                table: "Organizations",
                column: "CachedSubscriptionStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_Name",
                table: "Organizations",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_SetupStep",
                table: "Organizations",
                column: "SetupStep");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_Slug",
                table: "Organizations",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_StoreCode",
                table: "Organizations",
                column: "StoreCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_StripeCustomerId",
                table: "Organizations",
                column: "StripeCustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_StripeSubscriptionId",
                table: "Organizations",
                column: "StripeSubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_Subdomain",
                table: "Organizations",
                column: "Subdomain",
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_Payouts_ConsignorId_PeriodStart_PeriodEnd",
                table: "Payouts",
                columns: new[] { "ConsignorId", "PeriodStart", "PeriodEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_Payouts_OrganizationId",
                table: "Payouts",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_PayoutSettings_OrganizationId",
                table: "PayoutSettings",
                column: "OrganizationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayoutSummaries_ConsignorId",
                table: "PayoutSummaries",
                column: "ConsignorId");

            migrationBuilder.CreateIndex(
                name: "IX_PayoutSummaries_LastComputedAt",
                table: "PayoutSummaries",
                column: "LastComputedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PayoutSummaries_OrganizationId",
                table: "PayoutSummaries",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_PayoutSummaries_OrganizationId_ConsignorId",
                table: "PayoutSummaries",
                columns: new[] { "OrganizationId", "ConsignorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayoutSummaries_OrganizationId_MeetsMinimumThreshold",
                table: "PayoutSummaries",
                columns: new[] { "OrganizationId", "MeetsMinimumThreshold" });

            migrationBuilder.CreateIndex(
                name: "IX_PendingConsolidatedNotifications_ConsignorId",
                table: "PendingConsolidatedNotifications",
                column: "ConsignorId");

            migrationBuilder.CreateIndex(
                name: "IX_PendingConsolidatedNotifications_OrganizationId",
                table: "PendingConsolidatedNotifications",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_PendingImportItems_ConsignorId",
                table: "PendingImportItems",
                column: "ConsignorId");

            migrationBuilder.CreateIndex(
                name: "IX_PendingImportItems_CreatedAt",
                table: "PendingImportItems",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PendingImportItems_ImportedItemId",
                table: "PendingImportItems",
                column: "ImportedItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PendingImportItems_OrganizationId",
                table: "PendingImportItems",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_PendingImportItems_OrganizationId_Status_Source",
                table: "PendingImportItems",
                columns: new[] { "OrganizationId", "Status", "Source" });

            migrationBuilder.CreateIndex(
                name: "IX_PendingImportItems_Source",
                table: "PendingImportItems",
                column: "Source");

            migrationBuilder.CreateIndex(
                name: "IX_PendingImportItems_SourceReference",
                table: "PendingImportItems",
                column: "SourceReference");

            migrationBuilder.CreateIndex(
                name: "IX_PendingImportItems_Status",
                table: "PendingImportItems",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PendingSquareImports_ImportedAt",
                table: "PendingSquareImports",
                column: "ImportedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PendingSquareImports_OrganizationId",
                table: "PendingSquareImports",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_PendingSquareImports_OrganizationId_SquareCatalogId",
                table: "PendingSquareImports",
                columns: new[] { "OrganizationId", "SquareCatalogId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PendingSquareTransactionItems_ItemId",
                table: "PendingSquareTransactionItems",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PendingSquareTransactionItems_OrganizationId",
                table: "PendingSquareTransactionItems",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_PendingSquareTransactionItems_OrganizationId_SquareCatalogId",
                table: "PendingSquareTransactionItems",
                columns: new[] { "OrganizationId", "SquareCatalogId" });

            migrationBuilder.CreateIndex(
                name: "IX_PendingSquareTransactionItems_PendingSquareImportId",
                table: "PendingSquareTransactionItems",
                column: "PendingSquareImportId");

            migrationBuilder.CreateIndex(
                name: "IX_PendingSquareTransactionItems_PendingTransactionId",
                table: "PendingSquareTransactionItems",
                column: "PendingTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_PendingSquareTransactions_OrganizationId",
                table: "PendingSquareTransactions",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_PendingSquareTransactions_OrganizationId_SquarePaymentId",
                table: "PendingSquareTransactions",
                columns: new[] { "OrganizationId", "SquarePaymentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PendingSquareTransactions_Status",
                table: "PendingSquareTransactions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PendingSquareTransactions_TransactionDate",
                table: "PendingSquareTransactions",
                column: "TransactionDate");

            migrationBuilder.CreateIndex(
                name: "IX_QBSyncLogs_ShopId_EntityId_Action",
                table: "QBSyncLogs",
                columns: new[] { "ShopId", "EntityId", "Action" });

            migrationBuilder.CreateIndex(
                name: "IX_Shoppers_OrganizationId",
                table: "Shoppers",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Shoppers_OrganizationId_Email",
                table: "Shoppers",
                columns: new[] { "OrganizationId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Shoppers_UserId",
                table: "Shoppers",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingCarts_CustomerId",
                table: "ShoppingCarts",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingCarts_ExpiresAt",
                table: "ShoppingCarts",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingCarts_OrganizationId",
                table: "ShoppingCarts",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingCarts_OrganizationId_CustomerId",
                table: "ShoppingCarts",
                columns: new[] { "OrganizationId", "CustomerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingCarts_OrganizationId_SessionId",
                table: "ShoppingCarts",
                columns: new[] { "OrganizationId", "SessionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingCarts_SessionId",
                table: "ShoppingCarts",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SquareConnections_OrganizationId",
                table: "SquareConnections",
                column: "OrganizationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SquareSyncLogs_OrganizationId",
                table: "SquareSyncLogs",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_SquareSyncLogs_SquareConnectionId",
                table: "SquareSyncLogs",
                column: "SquareConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_SquareSyncLogs_SyncStarted",
                table: "SquareSyncLogs",
                column: "SyncStarted");

            migrationBuilder.CreateIndex(
                name: "IX_Statements_ConsignorId",
                table: "Statements",
                column: "ConsignorId");

            migrationBuilder.CreateIndex(
                name: "IX_Statements_ConsignorId_PeriodStart",
                table: "Statements",
                columns: new[] { "ConsignorId", "PeriodStart" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_Statements_OrganizationId",
                table: "Statements",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Statements_OrganizationId_ConsignorId_PeriodStart",
                table: "Statements",
                columns: new[] { "OrganizationId", "ConsignorId", "PeriodStart" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionEvents_OrganizationId",
                table: "SubscriptionEvents",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionEvents_StripeEventId",
                table: "SubscriptionEvents",
                column: "StripeEventId",
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_TransactionItems_ConsignorId",
                table: "TransactionItems",
                column: "ConsignorId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionItems_ItemId",
                table: "TransactionItems",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionItems_OrganizationId",
                table: "TransactionItems",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionItems_TransactionId",
                table: "TransactionItems",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ConsignorId",
                table: "Transactions",
                column: "ConsignorId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_OrderId",
                table: "Transactions",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_OrganizationId",
                table: "Transactions",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_PayoutId",
                table: "Transactions",
                column: "PayoutId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ProcessedByUserId",
                table: "Transactions",
                column: "ProcessedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_SaleDate",
                table: "Transactions",
                column: "SaleDate");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_SquarePaymentId",
                table: "Transactions",
                column: "SquarePaymentId",
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleAssignments_OrganizationId",
                table: "UserRoleAssignments",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleAssignments_UserId",
                table: "UserRoleAssignments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleAssignments_UserId_IsActive",
                table: "UserRoleAssignments",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleAssignments_UserId_Role_OrganizationId",
                table: "UserRoleAssignments",
                columns: new[] { "UserId", "Role", "OrganizationId" },
                unique: true);

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
                name: "AchTransfers");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "BookkeepingSettings");

            migrationBuilder.DropTable(
                name: "CartItems");

            migrationBuilder.DropTable(
                name: "ClerkInvitations");

            migrationBuilder.DropTable(
                name: "ClerkPermissions");

            migrationBuilder.DropTable(
                name: "ConsignorAgreements");

            migrationBuilder.DropTable(
                name: "ConsignorAgreementUploads");

            migrationBuilder.DropTable(
                name: "ConsignorInvitations");

            migrationBuilder.DropTable(
                name: "CustomerWishlist");

            migrationBuilder.DropTable(
                name: "DropoffRequests");

            migrationBuilder.DropTable(
                name: "FileUploadHashes");

            migrationBuilder.DropTable(
                name: "GuestCheckouts");

            migrationBuilder.DropTable(
                name: "IntegrationCredentials");

            migrationBuilder.DropTable(
                name: "ItemImages");

            migrationBuilder.DropTable(
                name: "ItemRequestImages");

            migrationBuilder.DropTable(
                name: "ItemTagAssignments");

            migrationBuilder.DropTable(
                name: "NotificationPreferences");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "OrderItems");

            migrationBuilder.DropTable(
                name: "OwnerInvitations");

            migrationBuilder.DropTable(
                name: "PaymentGatewayConnections");

            migrationBuilder.DropTable(
                name: "PayoutSettings");

            migrationBuilder.DropTable(
                name: "PayoutSummaries");

            migrationBuilder.DropTable(
                name: "PendingConsolidatedNotifications");

            migrationBuilder.DropTable(
                name: "PendingImportItems");

            migrationBuilder.DropTable(
                name: "PendingSquareTransactionItems");

            migrationBuilder.DropTable(
                name: "QBSyncLogs");

            migrationBuilder.DropTable(
                name: "Shoppers");

            migrationBuilder.DropTable(
                name: "SquareSyncLogs");

            migrationBuilder.DropTable(
                name: "SubscriptionEvents");

            migrationBuilder.DropTable(
                name: "Suggestions");

            migrationBuilder.DropTable(
                name: "SupportTickets");

            migrationBuilder.DropTable(
                name: "TransactionItems");

            migrationBuilder.DropTable(
                name: "UserNotificationPreferences");

            migrationBuilder.DropTable(
                name: "UserRoleAssignments");

            migrationBuilder.DropTable(
                name: "FundingSources");

            migrationBuilder.DropTable(
                name: "ShoppingCarts");

            migrationBuilder.DropTable(
                name: "ItemTags");

            migrationBuilder.DropTable(
                name: "ItemRequests");

            migrationBuilder.DropTable(
                name: "Statements");

            migrationBuilder.DropTable(
                name: "PendingSquareImports");

            migrationBuilder.DropTable(
                name: "PendingSquareTransactions");

            migrationBuilder.DropTable(
                name: "SquareConnections");

            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropTable(
                name: "ItemCategories");

            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "Payouts");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "Consignors");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Organizations");
        }
    }
}
