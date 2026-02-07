using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Application.DTOs.Shopper;

public class ShopperLoginRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; } = false;
}