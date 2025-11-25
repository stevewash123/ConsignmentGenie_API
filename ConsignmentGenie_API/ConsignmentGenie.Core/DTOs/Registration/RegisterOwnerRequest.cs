using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.DTOs.Registration;

public class RegisterOwnerRequest
{
    // Step 1: Account Info
    [Required]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string Password { get; set; } = string.Empty;

    public string? Phone { get; set; }

    // Step 2: Shop Setup
    [Required]
    public string ShopName { get; set; } = string.Empty;

    [Required]
    public string Address { get; set; } = string.Empty;

    [Required]
    public string ShopType { get; set; } = string.Empty;

    // Step 3: Business Details
    [Required, Range(10, 80)]
    public decimal DefaultCommissionRate { get; set; } = 50;

    public string? TaxId { get; set; }

    public string? ReturnPolicy { get; set; }

    public string? ConsignmentTerms { get; set; }
}