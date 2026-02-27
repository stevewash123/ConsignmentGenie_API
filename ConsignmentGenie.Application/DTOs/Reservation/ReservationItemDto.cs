namespace ConsignmentGenie.Application.DTOs.Reservation;

public class ReservationItemDto
{
    public Guid Id { get; set; }
    public Guid ReservationId { get; set; }
    public Guid ItemId { get; set; }

    // Price and item details at time of reservation
    public decimal ReservedPrice { get; set; }
    public string ItemTitle { get; set; } = string.Empty;
    public string? ItemSku { get; set; }
    public string? ItemImageUrl { get; set; }

    // Quantity (typically 1 for consignment items)
    public int Quantity { get; set; } = 1;

    // Notes specific to this item
    public string? Notes { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Extended item information (populated from Item entity)
    public string? ItemDescription { get; set; }
    public string? ItemBrand { get; set; }
    public string? ItemSize { get; set; }
    public string? ItemColor { get; set; }
    public string ItemCondition { get; set; } = string.Empty;

    // Consignor information
    public Guid ConsignorId { get; set; }
    public string ConsignorName { get; set; } = string.Empty;
}

public class CreateReservationItemRequest
{
    public Guid ItemId { get; set; }
    public int Quantity { get; set; } = 1;
    public string? Notes { get; set; }
}

public class UpdateReservationItemRequest
{
    public Guid Id { get; set; }
    public int Quantity { get; set; }
    public string? Notes { get; set; }
}