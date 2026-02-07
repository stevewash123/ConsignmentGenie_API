using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.DTOs.Settings;

/// <summary>
/// Organization business settings DTO
/// </summary>
public class OrganizationBusinessSettingsDto
{
    public decimal TaxRate { get; set; }
    public string? TaxIdEin { get; set; }
    public bool TaxIncludedInPrices { get; set; }
    public bool ChargeTaxOnShipping { get; set; }
    public decimal DefaultSplitPercentage { get; set; }
    public bool AllowCustomSplitsPerConsignor { get; set; }
    public bool AllowCustomSplitsPerItem { get; set; }
    public string? RefundPolicy { get; set; }
    public int? RefundWindowDays { get; set; }
    public int DefaultConsignmentPeriodDays { get; set; }
    public bool EnableAutoMarkdowns { get; set; }
    public string? ItemSubmissionMode { get; set; }
    public bool AutoApproveItems { get; set; }
}

/// <summary>
/// Request DTO for updating organization business settings
/// </summary>
public class UpdateOrganizationBusinessSettingsRequest
{
    public decimal? TaxRate { get; set; }
    public string? TaxIdEin { get; set; }
    public bool? TaxIncludedInPrices { get; set; }
    public bool? ChargeTaxOnShipping { get; set; }
    public decimal? DefaultSplitPercentage { get; set; }
    public bool? AllowCustomSplitsPerConsignor { get; set; }
    public bool? AllowCustomSplitsPerItem { get; set; }
    public string? RefundPolicy { get; set; }
    public int? RefundWindowDays { get; set; }
    public int? DefaultConsignmentPeriodDays { get; set; }
    public bool? EnableAutoMarkdowns { get; set; }
    public string? ItemSubmissionMode { get; set; }
    public bool? AutoApproveItems { get; set; }
}