using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.DTOs.Settings;

/// <summary>
/// Organization sales settings DTO
/// </summary>
public class OrganizationSalesSettingsDto
{
    // Online Storefront
    public bool StorefrontEnabled { get; set; }
    public string? StorefrontUrl { get; set; }
    public string? StorefrontTheme { get; set; }

    // Sales Policies
    public bool OnlineOrdersEnabled { get; set; }
    public bool InStorePickupEnabled { get; set; }
    public bool ShippingEnabled { get; set; }
    public decimal? MinimumOrderValue { get; set; }

    // Marketing & SEO
    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }
    public string? SeoKeywords { get; set; }

    // Analytics
    public string? GoogleAnalyticsId { get; set; }
    public string? FacebookPixelId { get; set; }
}

/// <summary>
/// Request DTO for updating organization sales settings
/// </summary>
public class UpdateOrganizationSalesSettingsRequest
{
    public bool? StorefrontEnabled { get; set; }
    public string? StorefrontUrl { get; set; }
    public string? StorefrontTheme { get; set; }
    public bool? OnlineOrdersEnabled { get; set; }
    public bool? InStorePickupEnabled { get; set; }
    public bool? ShippingEnabled { get; set; }
    public decimal? MinimumOrderValue { get; set; }
    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }
    public string? SeoKeywords { get; set; }
    public string? GoogleAnalyticsId { get; set; }
    public string? FacebookPixelId { get; set; }
}