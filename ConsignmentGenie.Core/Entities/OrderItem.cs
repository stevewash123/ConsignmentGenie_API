using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.Entities;

public class OrderItem : BaseEntity
{
    [Required]
    public Guid OrderId { get; set; }

    [Required]
    public Guid ItemId { get; set; }

    [Required]
    public Guid ConsignorId { get; set; }

    // Snapshot at time of order (prices can change)
    [Required]
    [MaxLength(200)]
    public string ItemName { get; set; } = string.Empty;

    [Required]
    public decimal ItemPrice { get; set; }

    // Consignor split (for transaction creation)
    [Required]
    [Range(0, 100)]
    public decimal SplitPercentage { get; set; }

    [Required]
    public decimal CommissionAmount { get; set; }

    // Navigation properties
    public Order Order { get; set; } = null!;
    public Item Item { get; set; } = null!;
    public Consignor Consignor { get; set; } = null!;

    // Computed properties - no quantity for consignment
    public decimal LineTotal => ItemPrice;
    public decimal ConsignorAmount => LineTotal - CommissionAmount;
}