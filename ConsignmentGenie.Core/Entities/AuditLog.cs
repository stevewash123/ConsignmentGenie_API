using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.Entities;

public class AuditLog : BaseEntity
{
    public Guid OrganizationId { get; set; }

    public Guid? UserId { get; set; } // Can be null for system actions

    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty; // Create, Update, Delete, Login, etc.

    [Required]
    [MaxLength(100)]
    public string EntityType { get; set; } = string.Empty; // Item, Consignor, Transaction, etc.

    public Guid? EntityId { get; set; } // ID of the affected entity

    [MaxLength(500)]
    public string? Description { get; set; }

    public string? OldValues { get; set; } // JSON of previous values

    public string? NewValues { get; set; } // JSON of new values

    [MaxLength(50)]
    public string? IpAddress { get; set; }

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    // Additional context
    public string? SessionId { get; set; }
    public string? CorrelationId { get; set; } // For tracking related operations

    // Risk assessment
    public string RiskLevel { get; set; } = "Low"; // Low, Medium, High

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public User? User { get; set; }
}