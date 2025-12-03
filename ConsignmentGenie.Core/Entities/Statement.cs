using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.Entities;

public class Statement : BaseEntity
{
    [Required]
    public Guid OrganizationId { get; set; }

    [Required]
    public Guid ConsignorId { get; set; }

    // Period
    [Required]
    [MaxLength(20)]
    public string StatementNumber { get; set; } = string.Empty; // STMT-2025-11-PRV00042

    [Required]
    public DateOnly PeriodStart { get; set; }

    [Required]
    public DateOnly PeriodEnd { get; set; }

    // Summary Figures
    public decimal OpeningBalance { get; set; } = 0m; // Balance at period start
    public decimal TotalSales { get; set; } = 0m; // Gross sales amount
    public decimal TotalEarnings { get; set; } = 0m; // Consignor's cut
    public decimal TotalPayouts { get; set; } = 0m; // Payouts during period
    public decimal ClosingBalance { get; set; } = 0m; // Balance at period end

    // Counts
    public int ItemsSold { get; set; } = 0;
    public int ItemsAdded { get; set; } = 0;
    public int ItemsRemoved { get; set; } = 0;
    public int PayoutCount { get; set; } = 0;

    // Storage
    [MaxLength(500)]
    public string? PdfUrl { get; set; } // Generated PDF location

    // Status
    [MaxLength(20)]
    public string Status { get; set; } = "Generated"; // Generated, Sent, Viewed

    public DateTime? ViewedAt { get; set; }

    // Audit
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public Consignor Consignor { get; set; } = null!;
}