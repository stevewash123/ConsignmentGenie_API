using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.Entities;

public class OwnerInvitation : BaseEntity
{
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

    // Navigation properties
    public User InvitedBy { get; set; } = null!;
}