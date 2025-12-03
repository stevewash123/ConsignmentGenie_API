namespace ConsignmentGenie.Core.DTOs.Items;

public class InventoryMetricsDto
{
    public int TotalItems { get; set; }
    public int AvailableItems { get; set; }
    public int SoldItems { get; set; }
    public int RemovedItems { get; set; }

    public decimal TotalValue { get; set; }          // Sum of available item prices
    public decimal AveragePrice { get; set; }

    public int ItemsAddedThisMonth { get; set; }
    public int ItemsSoldThisMonth { get; set; }

    public List<CategoryBreakdownDto> ByCategory { get; set; } = new();
    public List<ProviderBreakdownDto> ByProvider { get; set; } = new();
}

public class CategoryBreakdownDto
{
    public string Category { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Value { get; set; }
}

public class ProviderBreakdownDto
{
    public Guid ConsignorId { get; set; }
    public string ConsignorName { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Value { get; set; }
}