using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConsignmentGenie.Core.Entities;

public class ReservationItem : BaseEntity
{
    public Guid ReservationId { get; set; }
    public Guid ItemId { get; set; }

    // Price at time of reservation (for historical accuracy)
    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal ReservedPrice { get; set; }

    // Item details at time of reservation (for historical accuracy)
    [Required]
    [MaxLength(255)]
    public string ItemTitle { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? ItemSku { get; set; }

    [MaxLength(500)]
    public string? ItemImageUrl { get; set; }

    // Quantity (always 1 for consignment items, but included for flexibility)
    [Required]
    public int Quantity { get; set; } = 1;

    // Notes specific to this item in the reservation
    public string? Notes { get; set; }

    // Navigation properties
    public Reservation Reservation { get; set; } = null!;
    public Item Item { get; set; } = null!;
}