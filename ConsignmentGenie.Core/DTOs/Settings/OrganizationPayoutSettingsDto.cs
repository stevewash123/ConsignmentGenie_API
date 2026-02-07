using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.DTOs.Settings;

/// <summary>
/// Organization payout settings DTO
/// </summary>
public class OrganizationPayoutSettingsDto
{
    // Payment Method Preferences
    public string? DefaultPayoutMethod { get; set; }
    public bool AutoPayoutsEnabled { get; set; }
    public int PayoutScheduleDays { get; set; }
    public decimal MinimumPayoutAmount { get; set; }

    // Bank Connection Settings
    public bool PlaidEnabled { get; set; }
    public bool DwollaEnabled { get; set; }
    public string? BankConnectionProvider { get; set; }
    public string? DefaultBankAccountId { get; set; }

    // Processing Settings
    public bool HoldPayoutsForNewConsignors { get; set; }
    public int NewConsignorHoldDays { get; set; }
    public bool RequirePayoutApproval { get; set; }

    // Fees & Charges
    public decimal PayoutProcessingFee { get; set; }
    public bool ChargePayoutFeeToConsignor { get; set; }
}

/// <summary>
/// Request DTO for updating organization payout settings
/// </summary>
public class UpdateOrganizationPayoutSettingsRequest
{
    public string? DefaultPayoutMethod { get; set; }
    public bool? AutoPayoutsEnabled { get; set; }
    public int? PayoutScheduleDays { get; set; }
    public decimal? MinimumPayoutAmount { get; set; }
    public bool? PlaidEnabled { get; set; }
    public bool? DwollaEnabled { get; set; }
    public string? BankConnectionProvider { get; set; }
    public string? DefaultBankAccountId { get; set; }
    public bool? HoldPayoutsForNewConsignors { get; set; }
    public int? NewConsignorHoldDays { get; set; }
    public bool? RequirePayoutApproval { get; set; }
    public decimal? PayoutProcessingFee { get; set; }
    public bool? ChargePayoutFeeToConsignor { get; set; }
}