using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.DTOs.Organization;

public class ShopProfileDto
{
    [Required]
    [MaxLength(200)]
    public string ShopName { get; set; } = string.Empty;

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
    [EmailAddress]
    public string? ShopEmail { get; set; }

    [MaxLength(255)]
    [Url]
    public string? ShopWebsite { get; set; }

    [MaxLength(50)]
    public string ShopTimezone { get; set; } = "America/New_York";
}