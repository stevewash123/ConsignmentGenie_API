using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.DTOs.Registration;

public class RegisterOwnerRequest
{
    [Required]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string ShopName { get; set; } = string.Empty;

    public string? Phone { get; set; }
}