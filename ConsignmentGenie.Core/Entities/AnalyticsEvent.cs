using ConsignmentGenie.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.Entities;

public class AnalyticsEvent : BaseEntity
{
    [Required]
    public Guid OrganizationId { get; set; }

    [Required]
    public AnalyticsEventType EventType { get; set; }

    public string? EventData { get; set; } // JSON data

    public Guid? ItemId { get; set; }

    public Guid? CustomerId { get; set; }

    public Guid? UserId { get; set; }

    public Guid? ProviderId { get; set; }

    [MaxLength(50)]
    public string Source { get; set; } = "Web"; // POS, Web, Mobile, API

    [MaxLength(100)]
    public string? SessionId { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [MaxLength(45)]
    public string? IPAddress { get; set; }

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public Item? Item { get; set; }
    public Customer? Customer { get; set; }
    public User? User { get; set; }
    public Provider? Provider { get; set; }
}