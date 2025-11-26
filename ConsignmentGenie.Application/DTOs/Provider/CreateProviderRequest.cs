using ConsignmentGenie.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Application.DTOs.Provider;

public class CreateProviderRequest
{
    [Required]
    [MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Phone]
    public string? Phone { get; set; }

    [Range(0, 100)]
    public decimal DefaultSplitPercentage { get; set; } = 50.00m;

    public string? PaymentMethod { get; set; }

    public string? PaymentDetails { get; set; }

    public ProviderStatus Status { get; set; } = ProviderStatus.Active;

    public string? Notes { get; set; }
}