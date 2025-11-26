using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.Entities;

public class Shopper : BaseEntity
{
    public Guid OrganizationId { get; set; }

    public Guid UserId { get; set; }

    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    // Shipping Address (for faster checkout)
    [MaxLength(255)]
    public string? ShippingAddress1 { get; set; }

    [MaxLength(255)]
    public string? ShippingAddress2 { get; set; }

    [MaxLength(100)]
    public string? ShippingCity { get; set; }

    [MaxLength(50)]
    public string? ShippingState { get; set; }

    [MaxLength(20)]
    public string? ShippingZip { get; set; }

    // Preferences
    public bool EmailNotifications { get; set; } = true;

    // Audit
    public DateTime? LastLoginAt { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public User User { get; set; } = null!;
}