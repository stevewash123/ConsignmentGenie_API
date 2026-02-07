using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.DTOs.SetupWizard;

public class ShopProfileDto
{
    [Required]
    [MaxLength(200)]
    public string ShopName { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? ShopDescription { get; set; }

    public string? ShopLogoUrl { get; set; }
    public string? ShopBannerUrl { get; set; }

    // Contact Information
    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string ShopEmail { get; set; } = string.Empty;

    [Phone]
    [MaxLength(50)]
    public string? ShopPhone { get; set; }

    [Url]
    [MaxLength(255)]
    public string? ShopWebsite { get; set; }

    // Address
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

    [Required]
    [MaxLength(50)]
    public string ShopCountry { get; set; } = "US";

    [Required]
    [MaxLength(50)]
    public string ShopTimezone { get; set; } = "America/New_York";
}