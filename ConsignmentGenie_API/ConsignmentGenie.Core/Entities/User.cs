using ConsignmentGenie.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.Entities;

public class User : BaseEntity
{
    [Required]
    [EmailAddress]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Owner;

    public Guid OrganizationId { get; set; }

    // Registration approval fields (Phase 4)
    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Approved;  // Default existing users to approved

    public Guid? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public string? RejectedReason { get; set; }

    [MaxLength(100)]
    public string? FullName { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public Provider? Provider { get; set; }  // For Provider role users
}