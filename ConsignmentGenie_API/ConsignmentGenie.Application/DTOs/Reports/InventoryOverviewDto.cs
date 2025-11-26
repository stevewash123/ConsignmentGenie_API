namespace ConsignmentGenie.Application.DTOs.Reports;

public class InventoryOverviewDto
{
    public int TotalItems { get; set; }
    public int AvailableItems { get; set; }
    public int SoldItems { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal TotalInventoryValue { get; set; }
    public List<CategoryBreakdownDto> CategoryBreakdown { get; set; } = new();
    public List<ProviderBreakdownDto> ProviderBreakdown { get; set; } = new();
}

public class CategoryBreakdownDto
{
    public string Category { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Value { get; set; }
    public int SoldCount { get; set; }
}

public class ProviderBreakdownDto
{
    public Guid ProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public int AvailableCount { get; set; }
    public int SoldCount { get; set; }
}