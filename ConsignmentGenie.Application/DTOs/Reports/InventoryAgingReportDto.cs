namespace ConsignmentGenie.Application.DTOs.Reports;

public class InventoryAgingReportDto
{
    public int TotalAvailable { get; set; }
    public int Over30Days { get; set; }
    public int Over60Days { get; set; }
    public int Over90Days { get; set; }
    public double AverageAge { get; set; }
    public List<AgingBucketDto> AgingBuckets { get; set; } = new();
    public List<AgingItemDto> Items { get; set; } = new();
}

public class AgingBucketDto
{
    public string Bucket { get; set; } = string.Empty; // "0-30", "31-60", "61-90", "90+"
    public int Count { get; set; }
    public decimal Value { get; set; }
}

public class AgingItemDto
{
    public Guid ItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string ConsignorName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateOnly ListedDate { get; set; }
    public int DaysListed { get; set; }
    public string SuggestedAction { get; set; } = string.Empty;
}