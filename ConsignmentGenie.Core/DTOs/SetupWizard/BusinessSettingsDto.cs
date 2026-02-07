using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.DTOs.SetupWizard;

public class BusinessSettingsDto
{
    [Required]
    [Range(0, 100)]
    public decimal DefaultSplitPercentage { get; set; } = 60.00m;

    [Required]
    [Range(0, 1)]
    public decimal TaxRate { get; set; } = 0.0000m;

    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";
}

public class StorefrontSettingsDto
{
    [MaxLength(100)]
    public string? Slug { get; set; }

    public bool StoreEnabled { get; set; } = false;

    // Fulfillment Options
    public bool ShippingEnabled { get; set; } = false;

    [Range(0, double.MaxValue)]
    public decimal ShippingFlatRate { get; set; } = 0m;

    public bool PickupEnabled { get; set; } = true;

    [MaxLength(500)]
    public string? PickupInstructions { get; set; }

    // Payment Options
    public bool PayOnPickupEnabled { get; set; } = true;
    public bool OnlinePaymentEnabled { get; set; } = false;
}