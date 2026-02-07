using System.ComponentModel.DataAnnotations;
using ConsignmentGenie.Core.Enums;

namespace ConsignmentGenie.Core.Entities;

public class Suggestion : BaseEntity
{
    public Guid OrganizationId { get; set; }

    public Guid UserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string UserEmail { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string UserName { get; set; } = string.Empty;

    [Required]
    public SuggestionType Type { get; set; }

    [Required]
    [MaxLength(2000)]
    public string Message { get; set; } = string.Empty;

    // Status tracking
    public bool IsProcessed { get; set; } = false;
    public DateTime? ProcessedAt { get; set; }
    public string? AdminNotes { get; set; }

    // Email tracking
    public bool EmailSent { get; set; } = false;
    public DateTime? EmailSentAt { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public User User { get; set; } = null!;
}