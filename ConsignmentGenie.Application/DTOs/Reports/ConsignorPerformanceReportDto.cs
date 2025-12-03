namespace ConsignmentGenie.Application.DTOs.Reports;

public class ProviderPerformanceReportDto
{
    public int TotalProviders { get; set; }
    public decimal TotalSales { get; set; }
    public decimal AverageSalesPerProvider { get; set; }
    public string TopProviderName { get; set; } = string.Empty;
    public decimal TopProviderSales { get; set; }
    public List<ProviderPerformanceLineDto> Consignors { get; set; } = new();
}

public class ProviderPerformanceLineDto
{
    public Guid ConsignorId { get; set; }
    public string ConsignorName { get; set; } = string.Empty;
    public int ItemsConsigned { get; set; }
    public int ItemsSold { get; set; }
    public int ItemsAvailable { get; set; }
    public decimal TotalSales { get; set; }
    public decimal SellThroughRate { get; set; }
    public double AvgDaysToSell { get; set; }
    public decimal PendingPayout { get; set; }
}