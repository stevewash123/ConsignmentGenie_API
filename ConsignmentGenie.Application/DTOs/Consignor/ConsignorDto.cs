using ConsignmentGenie.Core.Enums;

namespace ConsignmentGenie.Application.DTOs.Consignor;

public class ConsignorDto
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public decimal DefaultSplitPercentage { get; set; }
    public string? PaymentMethod { get; set; }
    public ConsignorStatus Status { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ActiveItemsCount { get; set; }
    public decimal TotalEarnings { get; set; }
}