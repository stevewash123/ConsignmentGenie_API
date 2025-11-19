using ConsignmentGenie.Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConsignmentGenie.Core.Entities;

public class Provider : BaseEntity
{
    public Guid OrganizationId { get; set; }

    public Guid? UserId { get; set; }  // Nullable - linked when portal access granted (Phase 3)

    [Required]
    [MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal DefaultSplitPercentage { get; set; } = 50.00m;  // e.g., 50 = 50%

    [MaxLength(50)]
    public string? PaymentMethod { get; set; }  // Venmo, Zelle, Check, BankTransfer

    public string? PaymentDetails { get; set; }  // JSON: venmo handle, etc.

    public ProviderStatus Status { get; set; } = ProviderStatus.Active;

    public string? Notes { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public User? User { get; set; }
    public ICollection<Item> Items { get; set; } = new List<Item>();
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<Payout> Payouts { get; set; } = new List<Payout>();
}