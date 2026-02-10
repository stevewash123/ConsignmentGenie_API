using System.ComponentModel.DataAnnotations;
using ConsignmentGenie.Core.Enums;

namespace ConsignmentGenie.Core.DTOs.Organization;

public class ConsignorOnboardingDto
{
    public AgreementRequirement AgreementRequirement { get; set; } = AgreementRequirement.Upload;

    public Guid? AgreementTemplateId { get; set; }

    public string? AcknowledgeTermsText { get; set; }

    public ApprovalMode ApprovalMode { get; set; } = ApprovalMode.Manual;

    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Request DTO for updating consignor onboarding settings (partial updates)
/// </summary>
public class UpdateConsignorOnboardingRequest
{
    public AgreementRequirement? AgreementRequirement { get; set; }

    public Guid? AgreementTemplateId { get; set; }

    public string? AcknowledgeTermsText { get; set; }

    public ApprovalMode? ApprovalMode { get; set; }
}