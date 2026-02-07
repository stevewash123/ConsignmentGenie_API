namespace ConsignmentGenie.Core.DTOs.Items;

public class ItemQueryParams
{
    public string? Search { get; set; }           // Search title, SKU, description
    public string? Status { get; set; }           // Available, Sold, Removed, or null for all
    public Guid? ConsignorId { get; set; }
    public string? Category { get; set; }
    public string? Condition { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public DateTime? ReceivedAfter { get; set; }
    public DateTime? ReceivedBefore { get; set; }
    public string SortBy { get; set; } = "CreatedAt";  // CreatedAt, Title, Price, ReceivedDate
    public string SortDirection { get; set; } = "desc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}