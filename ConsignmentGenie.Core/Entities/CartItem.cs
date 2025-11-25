using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.Entities;

public class CartItem : BaseEntity
{
    [Required]
    public Guid CartId { get; set; }

    [Required]
    public Guid ItemId { get; set; }

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ShoppingCart Cart { get; set; } = null!;
    public Item Item { get; set; } = null!;

    // Computed properties - no quantity for consignment items
    public decimal LineTotal => Item.Price;
}