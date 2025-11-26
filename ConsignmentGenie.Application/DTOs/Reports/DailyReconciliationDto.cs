namespace ConsignmentGenie.Application.DTOs.Reports;

public class DailyReconciliationDto
{
    public DateOnly Date { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal CashSales { get; set; }
    public decimal CardSales { get; set; }
    public decimal CheckSales { get; set; }
    public decimal OtherSales { get; set; }
    public decimal TotalSales { get; set; }
    public decimal ExpectedCash { get; set; }
    public decimal? ActualCash { get; set; }
    public decimal? Variance { get; set; }
    public string Notes { get; set; } = string.Empty;
    public List<ReconciliationLineDto> Transactions { get; set; } = new();
}

public class ReconciliationLineDto
{
    public DateTime Time { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public string Items { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class DailyReconciliationRequestDto
{
    public DateOnly Date { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal ActualCash { get; set; }
    public string Notes { get; set; } = string.Empty;
}