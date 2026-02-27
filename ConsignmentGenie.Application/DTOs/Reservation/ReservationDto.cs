using ConsignmentGenie.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Application.DTOs.Reservation;

public class ReservationDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }

    // Customer Information
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }

    // Verification Status
    public bool IsPhoneVerified { get; set; }
    public DateTime? PhoneVerifiedAt { get; set; }

    // Reservation Details
    public ReservationStatus Status { get; set; }
    public DateTime? StatusChangedAt { get; set; }
    public string? StatusChangedReason { get; set; }

    // Timing
    public DateTime ExpiresAt { get; set; }
    public DateTime? PickedUpAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    // Notes
    public string? CustomerNotes { get; set; }
    public string? InternalNotes { get; set; }

    // Total
    public decimal TotalValue { get; set; }

    // Instructions
    public string? PickupInstructions { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    // Items
    public List<ReservationItemDto> Items { get; set; } = new();

    // Helper properties
    public int ItemCount => Items.Count;
    public bool IsExpired => Status != ReservationStatus.Completed &&
                           Status != ReservationStatus.Cancelled &&
                           Status != ReservationStatus.CancelledByStaff &&
                           DateTime.UtcNow > ExpiresAt;
    public TimeSpan TimeRemaining => ExpiresAt - DateTime.UtcNow;
}

public class CreateReservationRequest
{
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

    [Required]
    public List<Guid> ItemIds { get; set; } = new();

    public string? CustomerNotes { get; set; }

    public int HoldHours { get; set; } = 24; // Default 24 hour hold
}

public class SendVerificationCodeRequest
{
    [Required]
    public Guid ReservationId { get; set; }
}

public class VerifyPhoneRequest
{
    [Required]
    public Guid ReservationId { get; set; }

    [Required]
    [MaxLength(6)]
    public string VerificationCode { get; set; } = string.Empty;
}

public class UpdateReservationStatusRequest
{
    [Required]
    public Guid ReservationId { get; set; }

    [Required]
    public ReservationStatus Status { get; set; }

    public string? StatusChangedReason { get; set; }

    public string? InternalNotes { get; set; }
}

public class ReservationSummaryDto
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public ReservationStatus Status { get; set; }
    public DateTime ExpiresAt { get; set; }
    public decimal TotalValue { get; set; }
    public int ItemCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsExpired { get; set; }
}