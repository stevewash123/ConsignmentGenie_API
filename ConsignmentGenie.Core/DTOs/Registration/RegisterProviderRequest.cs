using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.DTOs.Registration;

public class RegisterProviderRequest
{
    [Required]
    public string StoreCode { get; set; } = string.Empty;

    [Required]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string Password { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? PreferredPaymentMethod { get; set; }

    public string? PaymentDetails { get; set; }
}