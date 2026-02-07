using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConsignmentGenie.Core.Entities;

public class TransactionItem : BaseEntity
{
    [Required]
    public Guid TransactionId { get; set; }

    [Required]
    public Guid OrganizationId { get; set; }

    [Required]
    public Guid ItemId { get; set; }

    [Required]
    public Guid ConsignorId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; set; } = 1;

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Unit price must be greater than 0")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal LineTotal => UnitPrice * Quantity;

    // Split calculation (calculated based on consignor's commission rate)
    [Required]
    [Column(TypeName = "decimal(5,4)")]
    public decimal ConsignorSplitPercentage { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal ConsignorAmount { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal StoreAmount { get; set; }

    // Navigation properties
    public Transaction Transaction { get; set; } = null!;
    public Organization Organization { get; set; } = null!;
    public Item Item { get; set; } = null!;
    public Consignor Consignor { get; set; } = null!;
}