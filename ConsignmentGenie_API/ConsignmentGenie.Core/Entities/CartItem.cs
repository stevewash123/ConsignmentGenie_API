using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.Entities;

public class CartItem : BaseEntity
{
    [Required]
    public Guid CartId { get; set; }

    [Required]
    public Guid ItemId { get; set; }

    [Required]
    [Range(1, 100)]
    public int Quantity { get; set; } = 1;

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ShoppingCart Cart { get; set; } = null!;
    public Item Item { get; set; } = null!;

    // Computed properties
    public decimal LineTotal => Item.Price * Quantity;
}