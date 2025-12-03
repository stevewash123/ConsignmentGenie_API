namespace ConsignmentGenie.Application.DTOs.Reports;

public class SalesReportDto
{
    public decimal TotalSales { get; set; }
    public decimal ShopRevenue { get; set; }
    public decimal ProviderPayable { get; set; }
    public int TransactionCount { get; set; }
    public decimal AverageSale { get; set; }
    public List<SalesChartPointDto> ChartData { get; set; } = new();
    public List<SalesLineItemDto> Transactions { get; set; } = new();
}

public class SalesChartPointDto
{
    public DateTime Date { get; set; }
    public decimal GrossSales { get; set; }
    public decimal ShopRevenue { get; set; }
    public decimal ProviderPayable { get; set; }
}

public class SalesLineItemDto
{
    public Guid TransactionId { get; set; }
    public DateTime Date { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string ConsignorName { get; set; } = string.Empty;
    public decimal SalePrice { get; set; }
    public decimal ShopCut { get; set; }
    public decimal ProviderCut { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
}