namespace ConsignmentGenie.Application.DTOs.Reports;

public class SalesReportFilterDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<Guid>? ProviderIds { get; set; }
    public List<string>? Categories { get; set; }
    public List<string>? PaymentMethods { get; set; }
}

public class ProviderPerformanceFilterDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IncludeInactive { get; set; } = false;
    public int? MinItemsThreshold { get; set; }
}

public class InventoryAgingFilterDto
{
    public int AgeThreshold { get; set; } = 30;
    public List<string>? Categories { get; set; }
    public List<Guid>? ProviderIds { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
}

public class PayoutSummaryFilterDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<Guid>? ProviderIds { get; set; }
    public string? Status { get; set; } // "Paid", "Pending", null for all
}