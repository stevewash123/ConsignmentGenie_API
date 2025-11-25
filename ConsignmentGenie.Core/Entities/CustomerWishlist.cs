using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.Entities;

public class CustomerWishlist : BaseEntity
{
    [Required]
    public Guid CustomerId { get; set; }

    [Required]
    public Guid ItemId { get; set; }

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Customer Customer { get; set; } = null!;
    public Item Item { get; set; } = null!;
}