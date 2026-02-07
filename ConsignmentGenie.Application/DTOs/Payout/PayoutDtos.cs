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
    public DateTime? ClearDate { get; set; }
    public bool IsCleared { get; set; }
    public decimal SalePrice { get; set; }
    public decimal ConsignorAmount { get; set; }
    public decimal ShopAmount { get; set; }
    public string? PaymentMethod { get; set; }
}

public class PayoutSummaryDto
{
    public Guid ConsignorId { get; set; }
    public string ConsignorName { get; set; } = string.Empty;
    public string? ConsignorNumber { get; set; }

    // Existing (keep for backwards compatibility)
    public decimal PendingAmount { get; set; }
    public int TransactionCount { get; set; }

    // NEW: Breakdown by clearance status
    public decimal ClearedAmount { get; set; }
    public decimal UnclearedAmount { get; set; }
    public decimal TotalPendingAmount { get; set; }
    public int ClearedTransactionCount { get; set; }
    public int UnclearedTransactionCount { get; set; }
    public int TotalTransactionCount { get; set; }

    // NEW: List includes clearance status
    public List<PayoutTransactionDto> Transactions { get; set; } = new();

    public DateTime? EarliestSale { get; set; }
    public DateTime? LatestSale { get; set; }
    public DateTime? EarliestClearDate { get; set; }  // When first uncleared will clear
    public DateTime? NextClearDate { get; set; }
    public bool MeetsMinimumThreshold { get; set; }
    public DateTime LastComputedAt { get; set; }
}

public class UpdatePayoutStatusRequest
{
    public ConsignmentGenie.Core.Enums.PayoutStatus Status { get; set; }
}