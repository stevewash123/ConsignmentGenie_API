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

    [MaxLength(500)]
    public string? QuickBooksRefreshToken { get; set; }  // Store encrypted

    public DateTime? QuickBooksTokenExpiry { get; set; }

    public DateTime? QuickBooksLastSync { get; set; }

    // Navigation properties
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Provider> Providers { get; set; } = new List<Provider>();
    public ICollection<Item> Items { get; set; } = new List<Item>();
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<Payout> Payouts { get; set; } = new List<Payout>();
}