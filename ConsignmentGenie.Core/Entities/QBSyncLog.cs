using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConsignmentGenie.Core.Entities;

public class QBSyncLog : BaseEntity
{
    [Required]
    public Guid ShopId { get; set; }

    [Required]
    [MaxLength(50)]
    public string EntityType { get; set; } = string.Empty; // 'Sale', 'Consignor', 'Payout'

    [Required]
    public Guid EntityId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty; // 'CreateSalesReceipt', 'CreateVendor', 'CreateBill', 'CreateBillPayment', 'CreateExpense'

    [MaxLength(100)]
    public string? QBId { get; set; } // ID returned by QuickBooks

    [Required]
    public bool Success { get; set; }

    public string? ErrorMessage { get; set; }

    // Navigation properties
    [ForeignKey(nameof(ShopId))]
    public Organization Organization { get; set; } = null!;
}