using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.Entities;

public enum InvitationStatus
{
    Pending,
    Accepted,
    Expired,
    Cancelled
}

public class ProviderInvitation : BaseEntity
{
    [Required]
    public Guid OrganizationId { get; set; }

    [Required]
    public Guid InvitedById { get; set; }

    [Required]
    [MaxLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string Token { get; set; } = string.Empty;

    public InvitationStatus Status { get; set; } = InvitationStatus.Pending;

    public DateTime ExpiresAt { get; set; }

    public DateTime ExpirationDate => ExpiresAt; // Compatibility property

    public bool IsUsed { get; set; } = false;

    public DateTime? UsedAt { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public User InvitedBy { get; set; } = null!;
}