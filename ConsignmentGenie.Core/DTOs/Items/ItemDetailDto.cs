using ConsignmentGenie.Core.Enums;

namespace ConsignmentGenie.Core.DTOs.Items;

// Detail View DTO (full item data)
public class ItemDetailDto
{
    public Guid ItemId { get; set; }
    public Guid ConsignorId { get; set; }
    public string ConsignorName { get; set; } = string.Empty;
    public decimal CommissionRate { get; set; }

    // Identification
    public string Sku { get; set; } = string.Empty;
    public string? Barcode { get; set; }

    // Details
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? Brand { get; set; }
    public string? Size { get; set; }
    public string? Color { get; set; }
    public ItemCondition Condition { get; set; }
    public string? Materials { get; set; }
    public string? Measurements { get; set; }

    // Pricing
    public decimal Price { get; set; }
    public decimal? OriginalPrice { get; set; }
    public decimal? MinimumPrice { get; set; }

    // Calculated
    public decimal ShopAmount { get; set; }      // Price * (1 - CommissionRate)
    public decimal ConsignorAmount { get; set; }  // Price * CommissionRate

    // Status
    public ItemStatus Status { get; set; }
    public DateTime? StatusChangedAt { get; set; }
    public string? StatusChangedReason { get; set; }

    // Dates
    public DateTime ReceivedDate { get; set; }
    public DateTime? ListedDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public DateTime? SoldDate { get; set; }

    // Photos
    public List<ItemImageDto> Images { get; set; } = new();

    // Location & Notes
    public string? Location { get; set; }
    public string? Notes { get; set; }
    public string? InternalNotes { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Related Transaction (if sold)
    public Guid? TransactionId { get; set; }
    public decimal? SalePrice { get; set; }
}