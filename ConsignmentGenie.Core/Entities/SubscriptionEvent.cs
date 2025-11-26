using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.Entities;

public class SubscriptionEvent : BaseEntity
{
    public Guid OrganizationId { get; set; }

    [Required]
    [MaxLength(100)]
    public string EventType { get; set; } = string.Empty;  // subscription.created, payment_succeeded, etc.

    [Required]
    [MaxLength(100)]
    public string StripeEventId { get; set; } = string.Empty;

    public string RawJson { get; set; } = string.Empty;  // Full webhook payload

    public bool Processed { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public string? ErrorMessage { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
}