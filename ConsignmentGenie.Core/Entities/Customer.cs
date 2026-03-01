using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.Entities;

public class Customer : BaseEntity
{
    [Required]
    [MaxLength(100)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    [Required]
    public Guid OrganizationId { get; set; }

    // OAuth Integration Fields
    [MaxLength(100)]
    public string? GoogleId { get; set; }

    [MaxLength(100)]
    public string? AppleId { get; set; }

    // Account Activity
    public DateTime? FirstReservationAt { get; set; }

    // Preferences (minimal for MVP)
    public bool NotifyReservationReminders { get; set; } = true;

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

    // Computed properties
    public string DisplayName => Name;
    public bool IsVerified => !string.IsNullOrEmpty(GoogleId) || !string.IsNullOrEmpty(AppleId) || !string.IsNullOrEmpty(PhoneNumber);
}