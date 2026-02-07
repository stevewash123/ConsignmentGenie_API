using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConsignmentGenie.Core.Entities;

public class BookkeepingSettings : BaseEntity
{
    [Required]
    public Guid OrganizationId { get; set; }

    // General Bookkeeping Software
    public bool UseQuickBooks { get; set; } = false;

    // QuickBooks Connection Status
    public bool QuickBooksConnected { get; set; } = false;

    [MaxLength(100)]
    public string? QuickBooksCompanyId { get; set; }

    [MaxLength(200)]
    public string? QuickBooksCompanyName { get; set; }

    public DateTime? QuickBooksLastSync { get; set; }

    // Sync Settings
    public bool SalesSyncEnabled { get; set; } = false;
    public bool ConsignorSyncEnabled { get; set; } = true;

    // Payout Recording Method
    [MaxLength(50)]
    public string PayoutRecordingMethod { get; set; } = "direct-expense"; // "bill-payment" | "direct-expense"

    // Line Item Detail Level
    [MaxLength(50)]
    public string LineItemDetail { get; set; } = "lump-sum"; // "lump-sum" | "itemized"

    // Sync Frequency
    [MaxLength(50)]
    public string SyncFrequency { get; set; } = "manual"; // "manual" | "daily" | "real-time"

    // Account Mappings (stored as JSON for flexibility)
    [Column(TypeName = "jsonb")]
    public string AccountMappings { get; set; } = "{}"; // { "salesAccount": "Sales Revenue", "expenseAccount": "Consignor Expenses", "payoutAccount": "Consignor Payouts" }

    // Sync Options
    public bool AutoCreateVendors { get; set; } = true; // Auto-create consignors as vendors in QuickBooks
    public bool SyncItemsAsProducts { get; set; } = false; // Sync individual items as products (can be overwhelming)
    public bool TrackInventoryQuantities { get; set; } = false; // Track item quantities in QuickBooks

    // Error Handling
    public bool ContinueOnSyncErrors { get; set; } = true; // Continue processing if individual items fail
    public DateTime? LastSyncAttempt { get; set; }
    public DateTime? LastSuccessfulSync { get; set; }
    public string? LastSyncError { get; set; }

    // Export Settings (when QuickBooks is disabled)
    public bool EnableCsvExport { get; set; } = true;
    public bool EnableExcelExport { get; set; } = true;

    [MaxLength(100)]
    public string? ExportFilePrefix { get; set; } = "ConsignmentGenie";

    // Navigation properties
    [ForeignKey(nameof(OrganizationId))]
    public Organization Organization { get; set; } = null!;
}