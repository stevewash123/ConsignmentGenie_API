using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.DTOs.Settings;

/// <summary>
/// Organization consignor settings DTO
/// </summary>
public class OrganizationConsignorSettingsDto
{
    // Consignor Onboarding
    public bool StoreCodeEnabled { get; set; }
    public string? StoreCode { get; set; }
    public string? RegistrationUrl { get; set; }

    // Consignor Agreement (from OrganizationSettingsDto)
    public bool AutoSendAgreementOnRegister { get; set; } = false;
    public bool RequireSignedAgreement { get; set; } = true;
    public string? AgreementTemplateId { get; set; } = null;
    public string? AgreementMethod { get; set; }
    public string? AcknowledgeTermsText { get; set; }

    // Default Terms
    public decimal ShopCommissionPercent { get; set; }
    public int ConsignmentPeriodDays { get; set; }
    public int RetrievalPeriodDays { get; set; }
    public string? UnsoldItemPolicy { get; set; }

    // Default Permissions
    public string? ApprovalMode { get; set; }
}

/// <summary>
/// Request DTO for updating organization consignor settings
/// </summary>
public class UpdateOrganizationConsignorSettingsRequest
{
    public bool? StoreCodeEnabled { get; set; }
    public string? StoreCode { get; set; }
    public bool? AutoSendAgreementOnRegister { get; set; }
    public bool? RequireSignedAgreement { get; set; }
    public string? AgreementTemplateId { get; set; }
    public string? AgreementMethod { get; set; }
    public string? AcknowledgeTermsText { get; set; }
    public decimal? ShopCommissionPercent { get; set; }
    public int? ConsignmentPeriodDays { get; set; }
    public int? RetrievalPeriodDays { get; set; }
    public string? UnsoldItemPolicy { get; set; }
    public string? ApprovalMode { get; set; }
}