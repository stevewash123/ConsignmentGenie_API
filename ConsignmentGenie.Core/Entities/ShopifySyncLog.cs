using ConsignmentGenie.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.Entities;

public class ShopifySyncLog : BaseEntity
{
    [Required]
    public Guid OrganizationId { get; set; }

    [Required]
    public ShopifySyncType SyncType { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    public bool Success { get; set; } = false;

    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }

    public int RecordsProcessed { get; set; } = 0;

    public int RecordsSucceeded { get; set; } = 0;

    public int RecordsFailed { get; set; } = 0;

    public string? Details { get; set; } // JSON: specific sync details

    [MaxLength(100)]
    public string? TriggeredBy { get; set; } // User ID or "System"

    // Navigation properties
    public Organization Organization { get; set; } = null!;

    // Computed properties
    public TimeSpan? Duration => CompletedAt?.Subtract(StartedAt);
    public bool IsInProgress => CompletedAt == null;
}