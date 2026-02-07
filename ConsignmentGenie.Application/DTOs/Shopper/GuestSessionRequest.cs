using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Application.DTOs.Shopper;

public class GuestSessionRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }
}