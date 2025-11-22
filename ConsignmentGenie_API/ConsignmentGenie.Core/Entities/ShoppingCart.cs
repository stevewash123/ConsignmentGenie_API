using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.Entities;

public class ShoppingCart : BaseEntity
{
    [Required]
    public Guid CustomerId { get; set; }

    [Required]
    public Guid OrganizationId { get; set; }

    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Customer Customer { get; set; } = null!;
    public Organization Organization { get; set; } = null!;
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    // Computed properties
    public decimal TotalAmount => CartItems.Sum(item => item.Item.Price * item.Quantity);
    public int ItemCount => CartItems.Sum(item => item.Quantity);
}