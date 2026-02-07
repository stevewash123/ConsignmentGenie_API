using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.DTOs.Settings;

/// <summary>
/// High-level Business page settings that wraps all business-related submenu DTOs
/// Maps to SettingsPage.Business and Organization.BusinessSettings JSON column
/// </summary>
public class BusinessPageSettingsDto
{
    public BusinessSettingsDto Business { get; set; } = new();
    public BusinessPoliciesDto ShopPolicies { get; set; } = new();
}

/// <summary>
/// Business settings (commission, tax, payouts, items) - part of the Business submenu
/// </summary>
public class BusinessSettingsDto
{
    public CommissionDto Commission { get; set; } = new();
    public TaxDto Tax { get; set; } = new();
    public PayoutDto Payouts { get; set; } = new();
    public ItemPolicyDto Items { get; set; } = new();
}

public class CommissionDto
{
    [Required]
    public string DefaultSplit { get; set; } = "60/40";
    public bool AllowCustomSplitsPerConsignor { get; set; } = false;
    public bool AllowCustomSplitsPerItem { get; set; } = false;
}

public class TaxDto
{
    [Range(0, 100)]
    public decimal SalesTaxRate { get; set; } = 0; // Percentage value (e.g., 8.25 for 8.25%)
    public bool TaxIncludedInPrices { get; set; } = false;
    public bool ChargeTaxOnShipping { get; set; } = false;
    [MaxLength(20)]
    public string? TaxIdEin { get; set; }
}

/// <summary>
/// Request DTO for updating business settings (partial updates with flat key-value structure)
/// </summary>
public class UpdateBusinessSettingsRequest
{
    public string? DefaultSplit { get; set; }
    public bool? AllowCustomSplitsPerConsignor { get; set; }
    public bool? AllowCustomSplitsPerItem { get; set; }

    public decimal? SalesTaxRate { get; set; }
    public bool? TaxIncludedInPrices { get; set; }
    public bool? ChargeTaxOnShipping { get; set; }
    public string? TaxIdEin { get; set; }

    public int? HoldPeriodDays { get; set; }
    public decimal? MinimumAmount { get; set; }
    public string? PayoutMethod { get; set; }
    public string? PayoutSchedule { get; set; }
    public bool? AutoProcessing { get; set; }
    public string? RefundPolicy { get; set; }
    public int? RefundWindowDays { get; set; }

    public int? DefaultConsignmentPeriodDays { get; set; }
    public bool? EnableAutoMarkdowns { get; set; }
    public string? ItemSubmissionMode { get; set; }
    public bool? AutoApproveItems { get; set; }
}

public class PayoutDto
{
    [Required]
    public string Schedule { get; set; } = "monthly";
    [Range(0, 10000)]
    public decimal MinimumAmount { get; set; } = 25.00m;
    [Range(0, 365)]
    public int HoldPeriodDays { get; set; } = 14;
    public string? Method { get; set; }
    public bool? AutoProcessing { get; set; }
    public string? RefundPolicy { get; set; }
    public int? RefundWindowDays { get; set; }
}

public class ItemPolicyDto
{
    [Range(30, 365)]
    public int DefaultConsignmentPeriodDays { get; set; } = 90;
    public bool EnableAutoMarkdowns { get; set; } = false;
    public MarkdownScheduleDto MarkdownSchedule { get; set; } = new();
    public string? ItemSubmissionMode { get; set; }
    public bool? AutoApproveItems { get; set; }
}

public class MarkdownScheduleDto
{
    [Range(0, 100)]
    public decimal After30Days { get; set; } = 0;
    [Range(0, 100)]
    public decimal After60Days { get; set; } = 0;
    [Required]
    public string After90DaysAction { get; set; } = "return";
}