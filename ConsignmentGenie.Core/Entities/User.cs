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

    /// <summary>
    /// Primary role for backward compatibility.
    /// New implementations should use RoleAssignments for multi-role support.
    /// </summary>
    public UserRole Role { get; set; } = UserRole.Owner;

    /// <summary>
    /// Primary organization for backward compatibility.
    /// New implementations should use RoleAssignments with OrganizationId.
    /// </summary>
    public Guid OrganizationId { get; set; }

    // Registration approval fields (Phase 4)
    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Approved;  // Default existing users to approved

    public Guid? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public string? RejectedReason { get; set; }

    [MaxLength(50)]
    public string? FirstName { get; set; }

    [MaxLength(50)]
    public string? LastName { get; set; }

    [MaxLength(100)]
    public string? FullName { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    // Clerk-specific fields
    [MaxLength(100)]
    public string? ClerkPin { get; set; }  // Optional clerk-level PIN (hashed)

    public bool IsActive { get; set; } = true;  // For deactivating clerks

    public DateTime? HiredDate { get; set; }  // When clerk was added

    public DateTime? LastLoginAt { get; set; }  // Track clerk login activity

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public Provider? Provider { get; set; }  // For Provider role users

    /// <summary>
    /// Multi-role assignments for this user across different organizations/contexts
    /// </summary>
    public ICollection<UserRoleAssignment> RoleAssignments { get; set; } = new List<UserRoleAssignment>();
}