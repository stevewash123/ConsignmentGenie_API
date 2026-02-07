using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.DTOs.Settings;

/// <summary>
/// Organization profile settings DTO
/// </summary>
public class OrganizationProfileSettingsDto
{
    public string? ShopName { get; set; }
    public string? ShopDescription { get; set; }
    public string? ShopLogoUrl { get; set; }
    public string? ShopBannerUrl { get; set; }
    public string? ShopPhone { get; set; }
    public string? ShopEmail { get; set; }
    public string? ShopWebsite { get; set; }
    public string? ShopAddress1 { get; set; }
    public string? ShopAddress2 { get; set; }
    public string? ShopCity { get; set; }
    public string? ShopState { get; set; }
    public string? ShopZip { get; set; }
    public string? ShopCountry { get; set; }
    public string? ShopTimezone { get; set; }
}

/// <summary>
/// Request DTO for updating organization profile settings
/// </summary>
public class UpdateOrganizationProfileSettingsRequest
{
    public string? ShopName { get; set; }
    public string? ShopDescription { get; set; }
    public string? ShopLogoUrl { get; set; }
    public string? ShopBannerUrl { get; set; }
    public string? ShopPhone { get; set; }
    public string? ShopEmail { get; set; }
    public string? ShopWebsite { get; set; }
    public string? ShopAddress1 { get; set; }
    public string? ShopAddress2 { get; set; }
    public string? ShopCity { get; set; }
    public string? ShopState { get; set; }
    public string? ShopZip { get; set; }
    public string? ShopCountry { get; set; }
    public string? ShopTimezone { get; set; }
}