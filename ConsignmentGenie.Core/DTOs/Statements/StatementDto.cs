namespace ConsignmentGenie.Core.DTOs.Statements;

public class StatementDto
{
    public Guid Id { get; set; }
    public string StatementNumber { get; set; } = string.Empty;

    // Period information
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public string PeriodLabel { get; set; } = string.Empty; // "November 2025"

    // Consignor and Organization info
    public string ConsignorName { get; set; } = string.Empty;
    public string ShopName { get; set; } = string.Empty;

    // Summary figures
    public decimal OpeningBalance { get; set; }
    public decimal TotalSales { get; set; }
    public decimal TotalEarnings { get; set; }
    public decimal TotalPayouts { get; set; }
    public decimal ClosingBalance { get; set; }

    // Counts
    public int ItemsSold { get; set; }
    public int ItemsAdded { get; set; }
    public int ItemsRemoved { get; set; }
    public int PayoutCount { get; set; }

    // Details
    public List<StatementSaleLineDto> Sales { get; set; } = new();
    public List<StatementPayoutLineDto> Payouts { get; set; } = new();

    // Status and metadata
    public string Status { get; set; } = string.Empty;
    public bool HasPdf { get; set; }
    public string? PdfUrl { get; set; }
    public DateTime? ViewedAt { get; set; }
    public DateTime GeneratedAt { get; set; }
}