using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.Entities;

public class GuestCheckout : BaseEntity
{
    public Guid OrganizationId { get; set; }

    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    [Required]
    [MaxLength(255)]
    public string SessionToken { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
}