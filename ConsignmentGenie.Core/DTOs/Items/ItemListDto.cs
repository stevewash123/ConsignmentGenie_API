using ConsignmentGenie.Core.Enums;

namespace ConsignmentGenie.Core.DTOs.Items;

// List View DTO (lightweight for table display)
public class ItemListDto
{
    public Guid ItemId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? Category { get; set; }
    public ItemCondition Condition { get; set; }
    public ItemStatus Status { get; set; }
    public string? PrimaryImageUrl { get; set; }
    public DateTime ReceivedDate { get; set; }
    public DateTime? SoldDate { get; set; }

    // Consignor info (denormalized for display)
    public Guid ConsignorId { get; set; }
    public string ConsignorName { get; set; } = string.Empty;
    public decimal CommissionRate { get; set; }
}