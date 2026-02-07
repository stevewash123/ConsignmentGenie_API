using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Application.DTOs.Shopper;

public class ShopperRegisterRequest
{
    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    public bool EmailNotifications { get; set; } = true;
}