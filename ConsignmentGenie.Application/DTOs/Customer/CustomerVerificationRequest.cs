using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Application.DTOs.Customer;

public class CustomerVerificationRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string VerificationMethod { get; set; } = string.Empty; // "google", "apple", "sms", "email"

    // For OAuth methods
    public string? OAuthToken { get; set; }

    // For SMS method
    public string? PhoneNumber { get; set; }
    public string? VerificationCode { get; set; }

    // For email method
    public string? EmailVerificationCode { get; set; }
}

public class CustomerVerificationResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public CustomerDto? Customer { get; set; }
    public bool RequiresCodeVerification { get; set; } // For SMS/email flows
    public string? SessionToken { get; set; } // For authenticated session
}