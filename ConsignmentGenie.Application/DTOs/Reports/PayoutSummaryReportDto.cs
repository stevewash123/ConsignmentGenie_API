namespace ConsignmentGenie.Application.DTOs.Reports;

public class PayoutSummaryReportDto
{
    public decimal TotalPaid { get; set; }
    public decimal TotalPending { get; set; }
    public int ProvidersWithPending { get; set; }
    public decimal AveragePayoutAmount { get; set; }
    public List<PayoutChartPointDto> ChartData { get; set; } = new();
    public List<PayoutSummaryLineDto> Consignors { get; set; } = new();
}

public class PayoutChartPointDto
{
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
}

public class PayoutSummaryLineDto
{
    public Guid ConsignorId { get; set; }
    public string ConsignorName { get; set; } = string.Empty;
    public decimal TotalSales { get; set; }
    public decimal ProviderCut { get; set; }
    public decimal AlreadyPaid { get; set; }
    public decimal PendingBalance { get; set; }
    public DateTime? LastPayoutDate { get; set; }
}