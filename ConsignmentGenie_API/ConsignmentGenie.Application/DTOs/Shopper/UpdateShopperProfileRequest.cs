using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Application.DTOs.Shopper;

public class UpdateShopperProfileRequest
{
    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    public AddressDto? ShippingAddress { get; set; }

    public bool EmailNotifications { get; set; }
}