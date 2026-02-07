using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Application.DTOs;

/// <summary>
/// DTO for consignor onboarding settings as specified in Story 01
/// </summary>
public class ConsignorOnboardingSettingsDto
{
    [Required]
    public string AgreementRequirement { get; set; } = "none";  // "none" | "acknowledge" | "upload"

    public Guid? AgreementTemplateId { get; set; }

    public string? AcknowledgeTermsText { get; set; }

    [Required]
    public string ApprovalMode { get; set; } = "manual";  // "auto" | "manual"
}

/// <summary>
/// Request DTO for updating consignor onboarding settings
/// </summary>
public class UpdateConsignorOnboardingSettingsRequest
{
    [Required]
    [RegularExpression("^(none|acknowledge|upload)$", ErrorMessage = "AgreementRequirement must be 'none', 'acknowledge', or 'upload'")]
    public string AgreementRequirement { get; set; } = "none";

    public Guid? AgreementTemplateId { get; set; }

    public string? AcknowledgeTermsText { get; set; }

    [Required]
    [RegularExpression("^(auto|manual)$", ErrorMessage = "ApprovalMode must be 'auto' or 'manual'")]
    public string ApprovalMode { get; set; } = "manual";
}