using ConsignmentGenie.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.Entities;

public class Organization : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public VerticalType VerticalType { get; set; } = VerticalType.Consignment;

    [MaxLength(50)]
    public string? Subdomain { get; set; }

    [MaxLength(100)]
    public string? Slug { get; set; }

    public string? Settings { get; set; }  // JSON: terminology mappings, defaults (maps to SettingsPage.Organization)

    public string? BusinessSettings { get; set; }  // JSON: comprehensive business configuration (maps to SettingsPage.Business)

    public string? StorefrontSettings { get; set; }  // JSON: storefront and integration settings (maps to SettingsPage.Storefront)

    public string? SalesSettings { get; set; }  // JSON: simplified sales management settings

    public string? ConsignorPermissions { get; set; }  // JSON: default consignor permissions (legacy - use ConsignorSettings)

    public string? ConsignorSettings { get; set; }  // JSON: consignor management and onboarding settings (maps to SettingsPage.Consignor)

    public string? NotificationSettings { get; set; }  // JSON: notification preferences and settings (maps to SettingsPage.Notifications)

    public string? ReceiptSettings { get; set; }  // JSON: receipt formatting and printing settings (maps to SettingsPage.Receipts)

    public string? BrandingSettings { get; set; }  // JSON: branding configuration including logo, colors, typography, and style settings

    // Stripe (Stripe is source of truth for subscription state)
    [MaxLength(255)]
    public string? StripeCustomerId { get; set; }

    [MaxLength(255)]
    public string? StripeSubscriptionId { get; set; }

    // Founder tracking (set once at first subscription)
    public bool IsFounderPricing { get; set; } = false;

    public int? FounderTier { get; set; }  // 1, 2, or NULL

    // Cached from Stripe for quick access (updated via webhooks)
    [MaxLength(50)]
    public string? CachedSubscriptionStatus { get; set; }  // 'trialing', 'active', 'canceled', etc.

    public DateTime? CachedPeriodEnd { get; set; }

    public string[]? ActiveIntegrations { get; set; }  // ['square_pos', 'quickbooks']

    // QuickBooks (Phase 3 - include fields now)
    public bool QuickBooksConnected { get; set; }

    [MaxLength(100)]
    public string? QuickBooksRealmId { get; set; }

    [MaxLength(500)]
    public string? QuickBooksAccessToken { get; set; }  // Store encrypted
    // TODO: Implement encryption before production (see TokenEncryptionService)

    [MaxLength(500)]
    public string? QuickBooksRefreshToken { get; set; }  // Store encrypted
    // TODO: Implement encryption before production (see TokenEncryptionService)

    public DateTime? QuickBooksTokenExpiry { get; set; }

    public DateTime? QuickBooksLastSync { get; set; }

    // Registration fields (Phase 4)
    [MaxLength(20)]
    public string? StoreCode { get; set; }

    public bool StoreCodeEnabled { get; set; } = true;

    public bool AutoApproveConsignors { get; set; } = true;  // MVP: auto-approve by default [DEPRECATED - use ApprovalMode]

    // Consignor Onboarding - Unified system from Stories 02-05
    public ApprovalMode ApprovalMode { get; set; } = ApprovalMode.Auto;  // How consignors are approved: Manual or Auto

    // Agreement Management - Three-tier system from Story 01
    [MaxLength(20)]
    public string AgreementMethod { get; set; } = "none";  // 'none', 'acknowledge', 'upload' - if not 'none', then agreements are required

    public string? AcknowledgeTermsText { get; set; }  // Text to display for acknowledge mode

    [MaxLength(500)]
    public string? AgreementTemplateCloudinaryUrl { get; set; }  // Cloudinary URL for uploaded agreement template in upload mode

    // Note: Trial & Subscription Status moved to Stripe (single source of truth)
    // For quick access without API calls, use CachedSubscriptionStatus above

    // Setup Progress
    public DateTime? SetupCompletedAt { get; set; }

    public int SetupStep { get; set; } = 0;  // Track wizard progress (0 = not started)

    // Onboarding
    public bool OnboardingDismissed { get; set; } = false;  // Track whether welcome modal was dismissed
    public bool WelcomeGuideCompleted { get; set; } = false;  // Track whether welcome guide was dismissed permanently
    public bool SetupChecklistDismissed { get; set; } = false;  // Track whether setup checklist was dismissed permanently

    // Shop Profile
    [MaxLength(200)]
    public string? ShopName { get; set; }

    public string? ShopDescription { get; set; }

    [MaxLength(500)]
    public string? ShopLogoUrl { get; set; }

    [MaxLength(500)]
    public string? ShopBannerUrl { get; set; }

    [MaxLength(200)]
    public string? ShopAddress1 { get; set; }

    [MaxLength(200)]
    public string? ShopAddress2 { get; set; }

    [MaxLength(100)]
    public string? ShopCity { get; set; }

    [MaxLength(50)]
    public string? ShopState { get; set; }

    [MaxLength(20)]
    public string? ShopZip { get; set; }

    [MaxLength(50)]
    public string ShopCountry { get; set; } = "US";

    [MaxLength(50)]
    public string? ShopPhone { get; set; }

    [MaxLength(255)]
    public string? ShopEmail { get; set; }

    [MaxLength(255)]
    public string? ShopWebsite { get; set; }

    [MaxLength(50)]
    public string ShopTimezone { get; set; } = "America/New_York";

    // Business Settings
    public decimal DefaultSplitPercentage { get; set; } = 60.00M;  // Consignor's cut

    public decimal TaxRate { get; set; } = 0.0000M;

    [MaxLength(10)]
    public string? ZipCode { get; set; }

    [MaxLength(3)]
    public string Currency { get; set; } = "USD";

    // Public Storefront

    public bool StoreEnabled { get; set; } = false;

    public bool ShippingEnabled { get; set; } = false;

    public decimal ShippingFlatRate { get; set; } = 0;

    public bool PickupEnabled { get; set; } = true;

    public string? PickupInstructions { get; set; }

    public bool PayOnPickupEnabled { get; set; } = true;

    public bool OnlinePaymentEnabled { get; set; } = false;

    // Security Settings - Owner PIN
    [MaxLength(100)]
    public string? OwnerPin { get; set; }  // Hashed PIN for approving restricted actions

    public bool RequirePinForTaxExempt { get; set; } = true;

    public decimal RequirePinForReturnsOver { get; set; } = 100.00M;

    public bool RequirePinForVoid { get; set; } = true;

    public bool RequirePinForDrawerOpen { get; set; } = true;

    // Integration Status (connection flags)
    [MaxLength(50)]
    public string? QuickBooksCompanyId { get; set; }

    public bool StripeConnected { get; set; } = false;  // For payment processing, not subscription

    [MaxLength(255)]
    public string? StripeAccountId { get; set; }  // Stripe Connect account ID for receiving customer payments

    [MaxLength(255)]
    public string? StripeAccessToken { get; set; }  // Stripe Connect access token (store encrypted in production)

    [MaxLength(255)]
    public string? StripeRefreshToken { get; set; }  // Stripe Connect refresh token (store encrypted in production)

    [MaxLength(255)]
    public string? StripePublishableKey { get; set; }  // Stripe Connect publishable key for frontend

    public DateTime? StripeConnectedAt { get; set; }  // When Stripe Connect was set up

    public bool StripePayoutsEnabled { get; set; } = false;  // Whether payouts are enabled on the connected account

    public bool SendGridConnected { get; set; } = false;

    public bool CloudinaryConnected { get; set; } = false;

    // Inventory Management (determines source of truth)
    public IntegrationMode IntegrationMode { get; set; } = IntegrationMode.CgNative;

    // Navigation properties
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Consignor> Consignors { get; set; } = new List<Consignor>();
    public ICollection<Item> Items { get; set; } = new List<Item>();
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<Payout> Payouts { get; set; } = new List<Payout>();
    public SquareConnection? SquareConnection { get; set; }
}