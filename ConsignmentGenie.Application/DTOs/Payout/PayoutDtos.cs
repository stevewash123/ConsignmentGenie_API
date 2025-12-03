namespace ConsignmentGenie.Application.DTOs.Payout;

public class PayoutReportDto
{
    public Guid ConsignorId { get; set; }
    public string ConsignorName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalAmount { get; set; }
    public int TransactionCount { get; set; }
    public string Status { get; set; } = string.Empty; // "Pending", "Paid", "Processing"
    public DateTime GeneratedAt { get; set; }
    public List<PayoutTransactionDto> Transactions { get; set; } = new();
}

public class PayoutTransactionDto
{
    public Guid TransactionId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; }
    public decimal SalePrice { get; set; }
    public decimal ConsignorAmount { get; set; }
    public decimal ShopAmount { get; set; }
}

public class PayoutSummaryDto
{
    public Guid ConsignorId { get; set; }
    public string ConsignorName { get; set; } = string.Empty;
    public decimal PendingAmount { get; set; }
    public int TransactionCount { get; set; }
    public DateTime OldestTransaction { get; set; }
}