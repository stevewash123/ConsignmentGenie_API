using ConsignmentGenie.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.Entities;

/// <summary>
/// Junction table to support multiple roles per user across different contexts/organizations
/// Enables scenarios like: User owns Shop A, consigns at Shop B, and shops at Shop C
/// </summary>
public class UserRoleAssignment : BaseEntity
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public UserRole Role { get; set; }

    /// <summary>
    /// The organization/shop context for this role.
    /// For Customer roles, this can be null if they shop across multiple shops.
    /// For Consignor/Owner roles, this specifies which shop they're associated with.
    /// </summary>
    public Guid? OrganizationId { get; set; }

    /// <summary>
    /// When this role assignment was created
    /// </summary>
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Who assigned this role (for audit purposes)
    /// </summary>
    public Guid? AssignedBy { get; set; }

    /// <summary>
    /// Whether this role is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Role-specific data as JSON (e.g., commission rates for providers, permissions for admins)
    /// </summary>
    [MaxLength(1000)]
    public string? RoleData { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public Organization? Organization { get; set; }
}