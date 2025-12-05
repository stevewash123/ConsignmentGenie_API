namespace ConsignmentGenie.Application.DTOs.Reports;

public class ConsignorPerformanceReportDto
{
    public int TotalConsignors { get; set; }
    public decimal TotalSales { get; set; }
    public decimal AverageSalesPerConsignor { get; set; }
    public string TopConsignorName { get; set; } = string.Empty;
    public decimal TopConsignorSales { get; set; }
    public List<ConsignorPerformanceLineDto> Consignors { get; set; } = new();
}

public class ConsignorPerformanceLineDto
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