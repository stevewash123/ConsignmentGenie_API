using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Application.DTOs.Shopper;

public class UpdateShopperProfileRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? PreferredName { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    public AddressDto? ShippingAddress { get; set; }

    public bool EmailNotifications { get; set; }
}