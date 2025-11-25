using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.Entities;

public class ShoppingCart : BaseEntity
{
    [Required]
    public Guid OrganizationId { get; set; }

    // Anonymous or logged in user support
    [MaxLength(100)]
    public string? SessionId { get; set; }  // For anonymous users

    public Guid? CustomerId { get; set; }  // For logged in users (nullable now)

    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }  // Auto-cleanup for abandoned carts

    // Navigation properties
    public Customer? Customer { get; set; }  // Nullable for anonymous carts
    public Organization Organization { get; set; } = null!;
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    // Computed properties - updated for consignment (no quantities)
    public decimal TotalAmount => CartItems.Sum(item => item.Item.Price);
    public int ItemCount => CartItems.Count;
}