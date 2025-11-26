using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.Entities;

public class StoreConfiguration : BaseEntity
{
    [Required]
    public Guid OrganizationId { get; set; }

    [Required]
    [MaxLength(100)]
    public string StoreName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? StoreUrl { get; set; }

    [MaxLength(500)]
    public string? LogoUrl { get; set; }

    public bool IsPublicStoreEnabled { get; set; } = true;

    [MaxLength(50)]
    public string Theme { get; set; } = "default";

    public string? ContactInfo { get; set; } // JSON: address, phone, hours, etc.

    public string? ShippingSettings { get; set; } // JSON: rates, zones, methods

    public string? TaxSettings { get; set; } // JSON: tax rates by location

    public string? SocialMediaLinks { get; set; } // JSON: Facebook, Instagram, etc.

    [MaxLength(1000)]
    public string? StoreDescription { get; set; }

    [MaxLength(500)]
    public string? StoreTagline { get; set; }

    public bool AllowWishlist { get; set; } = true;

    public bool AllowCustomerReviews { get; set; } = false;

    public bool RequireCustomerRegistration { get; set; } = false;

    [Range(0, 100)]
    public decimal DefaultTaxRate { get; set; } = 0;

    [MaxLength(3)]
    public string Currency { get; set; } = "USD";

    // Navigation properties
    public Organization Organization { get; set; } = null!;
}