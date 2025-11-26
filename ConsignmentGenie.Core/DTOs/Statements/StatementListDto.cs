namespace ConsignmentGenie.Core.DTOs.Statements;

public class StatementListDto
{
    public Guid StatementId { get; set; }
    public string StatementNumber { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public string PeriodLabel { get; set; } = string.Empty; // "November 2025"

    public int ItemsSold { get; set; }
    public decimal TotalEarnings { get; set; }
    public decimal ClosingBalance { get; set; }

    public string Status { get; set; } = string.Empty;
    public bool HasPdf { get; set; }
    public DateTime GeneratedAt { get; set; }
}