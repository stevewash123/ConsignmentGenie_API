using ConsignmentGenie.Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConsignmentGenie.Core.Entities;

public class Consignor : BaseEntity
{
    [Required]
    public Guid OrganizationId { get; set; }

    // Link to User (if provider has portal access)
    public Guid? UserId { get; set; }

    // Identity
    [Required]
    [MaxLength(20)]
    public string ConsignorNumber { get; set; } = string.Empty;  // Auto-generated: PRV-00001

    // Contact Info
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(255)]
    [EmailAddress]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    // Address (optional)
    [MaxLength(255)]
    public string? AddressLine1 { get; set; }

    [MaxLength(255)]
    public string? AddressLine2 { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(50)]
    public string? State { get; set; }

    [MaxLength(20)]
    public string? PostalCode { get; set; }

    // Business Terms
    [Required]
    [Column(TypeName = "decimal(5,4)")]
    public decimal CommissionRate { get; set; } = 0.5000m;  // 0.5000 = 50%

    public DateTime? ContractStartDate { get; set; }
    public DateTime? ContractEndDate { get; set; }

    // Payment Preferences
    [MaxLength(50)]
    public string? PreferredPaymentMethod { get; set; }  // Cash, Check, Venmo, Zelle, PayPal

    [MaxLength(255)]
    public string? PaymentDetails { get; set; }  // Venmo handle, Zelle email, etc.

    // Status
    [Required]
    [MaxLength(20)]
    public ConsignorStatus Status { get; set; } = ConsignorStatus.Active;

    public DateTime? StatusChangedAt { get; set; }

    [MaxLength(255)]
    public string? StatusChangedReason { get; set; }

    // Self-Registration (if applicable)
    [MaxLength(20)]
    public string? ApprovalStatus { get; set; }  // Pending, Approved, Rejected

    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedBy { get; set; }

    [MaxLength(255)]
    public string? RejectedReason { get; set; }

    // Notes
    public string? Notes { get; set; }  // Internal notes about this provider

    // Additional fields for compatibility
    [MaxLength(200)]
    public string? DisplayName { get; set; }

    [MaxLength(200)]
    public string? BusinessName { get; set; }

    [MaxLength(100)]
    public string? Address { get; set; } // Alias for AddressLine1

    [MaxLength(20)]
    public string? ZipCode { get; set; } // Alias for PostalCode

    [MaxLength(50)]
    public string? PaymentMethod { get; set; } // Alias for PreferredPaymentMethod

    [Column(TypeName = "decimal(5,4)")]
    public decimal? DefaultSplitPercentage { get; set; } // Alias for CommissionRate

    public bool PortalAccess { get; set; } = false; // Whether provider has portal access

    [MaxLength(50)]
    public string? InviteCode { get; set; } // Temporary invite code for portal access

    public DateTime? InviteExpiry { get; set; } // When the invite code expires

    // Audit fields (inherited from BaseEntity: Id, CreatedAt, UpdatedAt)
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public User? User { get; set; }
    public User? ApprovedByUser { get; set; }
    public User? CreatedByUser { get; set; }
    public User? UpdatedByUser { get; set; }
    public ICollection<Item> Items { get; set; } = new List<Item>();
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<Payout> Payouts { get; set; } = new List<Payout>();
}