using ConsignmentGenie.Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConsignmentGenie.Core.Entities;

public class Reservation : BaseEntity
{
    public Guid OrganizationId { get; set; }

    // Customer Information
    [Required]
    [MaxLength(100)]
    public string CustomerName { get; set; } = string.Empty;

    [Required]
    [MaxLength(15)]
    [Phone]
    public string CustomerPhone { get; set; } = string.Empty;

    [MaxLength(255)]
    [EmailAddress]
    public string? CustomerEmail { get; set; }

    // SMS Verification
    [MaxLength(6)]
    public string? VerificationCode { get; set; }

    public DateTime? VerificationCodeSentAt { get; set; }

    public bool IsPhoneVerified { get; set; } = false;

    public DateTime? PhoneVerifiedAt { get; set; }

    // Reservation Details
    [Required]
    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;

    public DateTime? StatusChangedAt { get; set; }

    [MaxLength(255)]
    public string? StatusChangedReason { get; set; }

    // Timing - 24 hour hold by default
    [Required]
    public DateTime ExpiresAt { get; set; }

    public DateTime? PickedUpAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    // Notes
    public string? CustomerNotes { get; set; }

    public string? InternalNotes { get; set; }

    // Total value for quick reference
    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalValue { get; set; }

    // Pickup/Contact Information
    [MaxLength(500)]
    public string? PickupInstructions { get; set; }

    // Audit
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public User? CreatedByUser { get; set; }
    public User? UpdatedByUser { get; set; }
    public ICollection<ReservationItem> ReservationItems { get; set; } = new List<ReservationItem>();
}