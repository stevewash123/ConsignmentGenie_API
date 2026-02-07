using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.DTOs.Settings;

/// <summary>
/// Organization bookkeeping settings DTO
/// </summary>
public class OrganizationBookkeepingSettingsDto
{
    // General Bookkeeping
    public string? DefaultExpenseCategory { get; set; }
    public string? DefaultIncomeCategory { get; set; }
    public bool TrackConsignorExpenses { get; set; }
    public bool GenerateEndOfYearReports { get; set; }

    // QuickBooks Integration
    public bool QuickBooksEnabled { get; set; }
    public string? QuickBooksCompanyId { get; set; }
    public bool AutoSyncTransactions { get; set; }
    public int SyncIntervalHours { get; set; }

    // Chart of Accounts Mapping
    public string? SalesAccountId { get; set; }
    public string? InventoryAccountId { get; set; }
    public string? PayoutAccountId { get; set; }
    public string? FeesAccountId { get; set; }

    // Reporting Settings
    public bool IncludeTaxInReports { get; set; }
    public string? ReportingCurrency { get; set; }
    public string? FiscalYearStartMonth { get; set; }
}

/// <summary>
/// Request DTO for updating organization bookkeeping settings
/// </summary>
public class UpdateOrganizationBookkeepingSettingsRequest
{
    public string? DefaultExpenseCategory { get; set; }
    public string? DefaultIncomeCategory { get; set; }
    public bool? TrackConsignorExpenses { get; set; }
    public bool? GenerateEndOfYearReports { get; set; }
    public bool? QuickBooksEnabled { get; set; }
    public string? QuickBooksCompanyId { get; set; }
    public bool? AutoSyncTransactions { get; set; }
    public int? SyncIntervalHours { get; set; }
    public string? SalesAccountId { get; set; }
    public string? InventoryAccountId { get; set; }
    public string? PayoutAccountId { get; set; }
    public string? FeesAccountId { get; set; }
    public bool? IncludeTaxInReports { get; set; }
    public string? ReportingCurrency { get; set; }
    public string? FiscalYearStartMonth { get; set; }
}