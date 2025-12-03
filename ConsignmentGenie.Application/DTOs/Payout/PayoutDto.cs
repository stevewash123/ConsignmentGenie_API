using ConsignmentGenie.Application.DTOs.Transaction;
using ConsignmentGenie.Core.Enums;

namespace ConsignmentGenie.Application.DTOs.Payout;

public class PayoutDto
{
    public Guid Id { get; set; }
    public string PayoutNumber { get; set; } = string.Empty;
    public DateTime PayoutDate { get; set; }
    public decimal Amount { get; set; }
    public PayoutStatus Status { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? PaymentReference { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int TransactionCount { get; set; }
    public string? Notes { get; set; }
    public bool SyncedToQuickBooks { get; set; }
    public string? QuickBooksBillId { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation data
    public ProviderSummaryDto Consignor { get; set; } = new();
    public List<PayoutTransactionDto> Transactions { get; set; } = new();
}

public class PayoutListDto
{
    public Guid Id { get; set; }
    public string PayoutNumber { get; set; } = string.Empty;
    public DateTime PayoutDate { get; set; }
    public decimal Amount { get; set; }
    public PayoutStatus Status { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int TransactionCount { get; set; }
    public ProviderSummaryDto Consignor { get; set; } = new();
}


