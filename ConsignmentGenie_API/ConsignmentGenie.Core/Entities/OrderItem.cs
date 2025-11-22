using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.Entities;

public class OrderItem : BaseEntity
{
    [Required]
    public Guid OrderId { get; set; }

    [Required]
    public Guid ItemId { get; set; }

    [Required]
    public Guid ProviderId { get; set; }

    [Required]
    [Range(1, 100)]
    public int Quantity { get; set; }

    [Required]
    public decimal Price { get; set; }

    [Required]
    public decimal CommissionAmount { get; set; }

    [Required]
    [Range(0, 100)]
    public decimal CommissionPercentage { get; set; }

    // Navigation properties
    public Order Order { get; set; } = null!;
    public Item Item { get; set; } = null!;
    public Provider Provider { get; set; } = null!;

    // Computed properties
    public decimal LineTotal => Price * Quantity;
    public decimal ProviderAmount => LineTotal - CommissionAmount;
}