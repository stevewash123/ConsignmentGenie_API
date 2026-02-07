using ConsignmentGenie.Core.Attributes;
using ConsignmentGenie.Core.Interfaces;
using ConsignmentGenie.Core.Models;
using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.DTOs.Settings;

/// <summary>
/// Default permissions for newly approved consignors
/// </summary>
public class ConsignorPermissions
{
    /// <summary>
    /// Whether consignors can add new items to the inventory
    /// </summary>
    public bool CanAddItems { get; set; } = true;

    /// <summary>
    /// Whether consignors can edit their own items
    /// </summary>
    public bool CanEditOwnItems { get; set; } = true;

    /// <summary>
    /// Whether consignors can remove/withdraw their own items
    /// </summary>
    public bool CanRemoveOwnItems { get; set; } = false;

    /// <summary>
    /// Whether consignors can edit item prices
    /// </summary>
    public bool CanEditPrices { get; set; } = true;

    /// <summary>
    /// Whether consignor accounts are active by default
    /// </summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Settings for the Consignor settings page
/// Manages onboarding, agreements, and approval workflows
/// </summary>
public class ConsignorPageSettings : IPageSettings
{
    /// <summary>
    /// How consignors are required to handle agreements
    /// Options: "none", "upload", "required"
    /// </summary>
    [Required]
    public string AgreementRequirement { get; set; } = "none";

    /// <summary>
    /// ID of the uploaded agreement template file
    /// Synced to Organization.AgreementTemplateId for business logic access
    /// </summary>
    [SyncToColumn(nameof(AgreementTemplateId))]
    public Guid? AgreementTemplateId { get; set; }

    /// <summary>
    /// Whether a signed agreement must be on file before consignor can add items
    /// Synced to Organization table for business logic queries
    /// </summary>
    [SyncToColumn("RequireSignedAgreement")]
    public bool RequireSignedAgreement { get; set; } = false;

    /// <summary>
    /// How new consignor registrations are approved
    /// Options: "auto", "manual"
    /// </summary>
    [Required]
    public string ApprovalMode { get; set; } = "auto";

    /// <summary>
    /// Custom text for terms acknowledgment checkbox
    /// If null/empty, default text is used
    /// </summary>
    public string? AcknowledgeTermsText { get; set; }

    /// <summary>
    /// Whether to automatically send agreement email on registration
    /// </summary>
    public bool AutoSendAgreementOnRegister { get; set; } = false;

    /// <summary>
    /// Whether to email shop owner when new consignor registers
    /// </summary>
    public bool EmailOnNewConsignor { get; set; } = true;

    /// <summary>
    /// Default permissions for newly approved consignors
    /// </summary>
    public ConsignorPermissions DefaultPermissions { get; set; } = new();

    /// <summary>
    /// Last updated timestamp
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Validate the current settings state
    /// </summary>
    public SettingsValidationResult Validate()
    {
        var errors = new List<string>();

        // Validate agreement requirement
        if (!new[] { "none", "upload", "required" }.Contains(AgreementRequirement))
        {
            errors.Add("AgreementRequirement must be 'none', 'upload', or 'required'");
        }

        // If agreement is required, template must be uploaded
        if (AgreementRequirement == "required" && !AgreementTemplateId.HasValue)
        {
            errors.Add("Agreement template is required when AgreementRequirement is 'required'");
        }

        // If signed agreements are required, must have upload requirement
        if (RequireSignedAgreement && AgreementRequirement == "none")
        {
            errors.Add("Cannot require signed agreements when AgreementRequirement is 'none'");
        }

        // Validate approval mode
        if (!new[] { "auto", "manual" }.Contains(ApprovalMode))
        {
            errors.Add("ApprovalMode must be 'auto' or 'manual'");
        }

        return errors.Count == 0 ? SettingsValidationResult.Success() : SettingsValidationResult.WithErrors(errors);
    }
}