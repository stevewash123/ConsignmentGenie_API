using System.Text.Json;

namespace ConsignmentGenie.Application.DTOs.Bookkeeping;

public class BookkeepingSettingsDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }

    // General Bookkeeping Software
    public bool UseQuickBooks { get; set; }

    // QuickBooks Connection Status
    public bool QuickBooksConnected { get; set; }
    public string? QuickBooksCompanyId { get; set; }
    public string? QuickBooksCompanyName { get; set; }
    public DateTime? QuickBooksLastSync { get; set; }

    // Sync Settings
    public bool SalesSyncEnabled { get; set; }
    public bool ConsignorSyncEnabled { get; set; }

    // Payout Recording Method
    public string PayoutRecordingMethod { get; set; } = "direct-expense";

    // Line Item Detail Level
    public string LineItemDetail { get; set; } = "lump-sum";

    // Sync Frequency
    public string SyncFrequency { get; set; } = "manual";

    // Account Mappings
    public string AccountMappings { get; set; } = "{}";

    // Sync Options
    public bool AutoCreateVendors { get; set; }
    public bool SyncItemsAsProducts { get; set; }
    public bool TrackInventoryQuantities { get; set; }

    // Error Handling
    public bool ContinueOnSyncErrors { get; set; }
    public DateTime? LastSyncAttempt { get; set; }
    public DateTime? LastSuccessfulSync { get; set; }
    public string? LastSyncError { get; set; }

    // Export Settings
    public bool EnableCsvExport { get; set; }
    public bool EnableExcelExport { get; set; }
    public string? ExportFilePrefix { get; set; }

    // Audit Fields
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateBookkeepingSettingsRequest
{
    // General Bookkeeping Software
    public bool UseQuickBooks { get; set; } = false;

    // QuickBooks Connection Status
    public bool QuickBooksConnected { get; set; } = false;
    public string? QuickBooksCompanyId { get; set; }
    public string? QuickBooksCompanyName { get; set; }

    // Sync Settings
    public bool SalesSyncEnabled { get; set; } = false;
    public bool ConsignorSyncEnabled { get; set; } = true;

    // Payout Recording Method
    public string PayoutRecordingMethod { get; set; } = "direct-expense";

    // Line Item Detail Level
    public string LineItemDetail { get; set; } = "lump-sum";

    // Sync Frequency
    public string SyncFrequency { get; set; } = "manual";

    // Account Mappings
    public string AccountMappings { get; set; } = "{}";

    // Sync Options
    public bool AutoCreateVendors { get; set; } = true;
    public bool SyncItemsAsProducts { get; set; } = false;
    public bool TrackInventoryQuantities { get; set; } = false;

    // Error Handling
    public bool ContinueOnSyncErrors { get; set; } = true;

    // Export Settings
    public bool EnableCsvExport { get; set; } = true;
    public bool EnableExcelExport { get; set; } = true;
    public string? ExportFilePrefix { get; set; } = "ConsignmentGenie";
}

public class UpdateBookkeepingSettingsRequest
{
    // General Bookkeeping Software
    public bool? UseQuickBooks { get; set; }

    // QuickBooks Connection Status
    public bool? QuickBooksConnected { get; set; }
    public string? QuickBooksCompanyId { get; set; }
    public string? QuickBooksCompanyName { get; set; }

    // Sync Settings
    public bool? SalesSyncEnabled { get; set; }
    public bool? ConsignorSyncEnabled { get; set; }

    // Payout Recording Method
    public string? PayoutRecordingMethod { get; set; }

    // Line Item Detail Level
    public string? LineItemDetail { get; set; }

    // Sync Frequency
    public string? SyncFrequency { get; set; }

    // Account Mappings
    public string? AccountMappings { get; set; }

    // Sync Options
    public bool? AutoCreateVendors { get; set; }
    public bool? SyncItemsAsProducts { get; set; }
    public bool? TrackInventoryQuantities { get; set; }

    // Error Handling
    public bool? ContinueOnSyncErrors { get; set; }
    public string? LastSyncError { get; set; }

    // Export Settings
    public bool? EnableCsvExport { get; set; }
    public bool? EnableExcelExport { get; set; }
    public string? ExportFilePrefix { get; set; }
}

// Account mappings helper class for strongly typed access
public class AccountMappings
{
    public string? SalesAccount { get; set; }
    public string? ExpenseAccount { get; set; }
    public string? PayoutAccount { get; set; }
}

// Simplified request DTO for partial updates matching UI auto-save pattern
public class PartialBookkeepingSettingsUpdate
{
    [System.Text.Json.Serialization.JsonExtensionData]
    public Dictionary<string, object>? AdditionalProperties { get; set; }
}