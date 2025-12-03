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

    public string? Settings { get; set; }  // JSON: terminology mappings, defaults

    // Stripe (Phase 2 - include fields now)
    [MaxLength(100)]
    public string? StripeCustomerId { get; set; }

    [MaxLength(100)]
    public string? StripeSubscriptionId { get; set; }

    [MaxLength(100)]
    public string? StripePriceId { get; set; }

    public SubscriptionStatus SubscriptionStatus { get; set; } = SubscriptionStatus.Trial;

    public SubscriptionTier SubscriptionTier { get; set; } = SubscriptionTier.Basic;

    public DateTime? SubscriptionStartDate { get; set; }

    public DateTime? SubscriptionEndDate { get; set; }

    public bool IsFounderPricing { get; set; }

    public int? FounderTier { get; set; }  // 1, 2, 3

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

    public bool AutoApproveConsignors { get; set; } = true;  // MVP: auto-approve by default

    // Trial & Subscription Status
    [MaxLength(20)]
    public string Status { get; set; } = "pending";  // pending, trial, active, suspended, cancelled

    public DateTime? TrialStartedAt { get; set; }

    public DateTime? TrialEndsAt { get; set; }

    public int TrialExtensionsUsed { get; set; } = 0;

    [MaxLength(50)]
    public string? StripeSubscriptionStatus { get; set; }

    [MaxLength(50)]
    public string? SubscriptionPlan { get; set; }  // 'basic', 'pro', 'enterprise'

    public DateTime? SubscriptionStartedAt { get; set; }

    public DateTime? CurrentPeriodEnd { get; set; }

    // Setup Progress
    public DateTime? SetupCompletedAt { get; set; }

    public int SetupStep { get; set; } = 0;  // Track wizard progress (0 = not started)

    // Onboarding
    public bool OnboardingDismissed { get; set; } = false;  // Track whether welcome modal was dismissed
    public bool WelcomeGuideCompleted { get; set; } = false;  // Track whether welcome guide was dismissed permanently

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

    // Integration Status (connection flags)
    [MaxLength(50)]
    public string? QuickBooksCompanyId { get; set; }

    public bool StripeConnected { get; set; } = false;  // For payment processing, not subscription

    public bool SendGridConnected { get; set; } = false;

    public bool CloudinaryConnected { get; set; } = false;

    // Navigation properties
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Consignor> Consignors { get; set; } = new List<Consignor>();
    public ICollection<Item> Items { get; set; } = new List<Item>();
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<Payout> Payouts { get; set; } = new List<Payout>();
}