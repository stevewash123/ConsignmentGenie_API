using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.DTOs.Settings;

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
    public decimal SalesTaxRate { get; set; } = 0;
    public bool TaxIncludedInPrices { get; set; } = false;
    public bool ChargeTaxOnShipping { get; set; } = false;
    [MaxLength(20)]
    public string? TaxIdEin { get; set; }
}

public class PayoutDto
{
    [Required]
    public string Schedule { get; set; } = "monthly";
    [Range(0, 10000)]
    public decimal MinimumAmount { get; set; } = 25.00m;
    [Range(0, 365)]
    public int HoldPeriodDays { get; set; } = 14;
}

public class ItemPolicyDto
{
    [Range(30, 365)]
    public int DefaultConsignmentPeriodDays { get; set; } = 90;
    public bool EnableAutoMarkdowns { get; set; } = false;
    public MarkdownScheduleDto MarkdownSchedule { get; set; } = new();
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