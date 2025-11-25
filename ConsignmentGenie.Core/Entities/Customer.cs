using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.Entities;

public class Customer : BaseEntity
{
    [Required]
    [MaxLength(100)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    [Required]
    public Guid OrganizationId { get; set; }

    [MaxLength(100)]
    public string? StripeCustomerId { get; set; }

    public bool IsEmailVerified { get; set; } = false;

    public DateTime? LastLoginAt { get; set; }

    // Password hash for customer authentication
    [MaxLength(256)]
    public string? PasswordHash { get; set; }

    public DateTime? EmailVerifiedAt { get; set; }

    [MaxLength(256)]
    public string? EmailVerificationToken { get; set; }

    [MaxLength(256)]
    public string? PasswordResetToken { get; set; }

    public DateTime? PasswordResetTokenExpiry { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public ICollection<ShoppingCart> ShoppingCarts { get; set; } = new List<ShoppingCart>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<CustomerWishlist> WishlistItems { get; set; } = new List<CustomerWishlist>();

    // Computed properties
    public string FullName => $"{FirstName} {LastName}";
}