using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ConsignmentGenie.Core.DTOs.Onboarding;
using ConsignmentGenie.Core.DTOs.Organization;
using ConsignmentGenie.Core.Enums;
using System.Text.Json;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/organizations/setup")]
[Authorize(Roles = "Owner")]
public class OrganizationSetupController : ControllerBase
{
    private readonly ConsignmentGenieContext _context;
    private readonly ILogger<OrganizationSetupController> _logger;

    public OrganizationSetupController(ConsignmentGenieContext context, ILogger<OrganizationSetupController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("status")]
    public async Task<ActionResult<object>> GetSetupStatus()
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[SETUP] Getting setup status for organization {OrganizationId}", organizationId);

        try
        {
            var organization = await _context.Organizations
                .Include(o => o.Consignors)
                .Include(o => o.Items)
                .Include(o => o.SquareConnection)
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                _logger.LogWarning("[SETUP] Organization {OrganizationId} not found", organizationId);
                return NotFound("Organization not found");
            }

            _logger.LogDebug("[SETUP] Organization {OrganizationId} found: Name={OrganizationName}, WelcomeGuideCompleted={WelcomeGuideCompleted}, ProviderCount={ProviderCount}, ItemCount={ItemCount}, StoreEnabled={StoreEnabled}, StripeConnected={StripeConnected}, QuickBooksConnected={QuickBooksConnected}",
                organizationId, organization.Name, organization.WelcomeGuideCompleted, organization.Consignors?.Count ?? 0, organization.Items?.Count ?? 0, organization.StoreEnabled, organization.StripeConnected, organization.QuickBooksConnected);

            var hasProviders = organization.Consignors.Any();
            var storefrontConfigured = organization.StoreEnabled ||
                                      organization.StripeConnected;
            var hasInventory = organization.Items.Any();
            var quickBooksConnected = organization.QuickBooksConnected;

            // New checklist fields - agreement is "complete" if disabled OR uploaded
            var agreementUploaded = organization.AgreementMethod == "none" || !string.IsNullOrEmpty(organization.AgreementTemplateCloudinaryUrl);
            var squareConnected = organization.SquareConnection != null;
            var payoutMethodConfigured = organization.StripeConnected; // Stripe handles both payment processing and payouts

            // NEW RULE: Once agreement step is handled (disabled OR uploaded), Welcome modal stops showing
            // The Setup Checklist card takes over for remaining optional steps
            var showModal = !organization.WelcomeGuideCompleted && !agreementUploaded;

            var status = new OnboardingStatusDto
            {
                Dismissed = organization.OnboardingDismissed,
                WelcomeGuideCompleted = organization.WelcomeGuideCompleted,
                ShowModal = showModal,
                SetupChecklistDismissed = organization.SetupChecklistDismissed,
                Steps = new OnboardingStepsDto
                {
                    HasProviders = hasProviders,
                    StorefrontConfigured = storefrontConfigured,
                    HasInventory = hasInventory,
                    QuickBooksConnected = quickBooksConnected,
                    AgreementUploaded = agreementUploaded,
                    SquareConnected = squareConnected,
                    PayoutMethodConfigured = payoutMethodConfigured
                }
            };

            _logger.LogInformation("[SETUP] Setup status calculated for organization {OrganizationId}: WelcomeGuideCompleted={WelcomeGuideCompleted}, ShowModal={ShowModal}, AgreementMethod={AgreementMethod}, AgreementUploaded={AgreementUploaded}, SetupChecklistDismissed={SetupChecklistDismissed}, SquareConnected={SquareConnected}, PayoutMethodConfigured={PayoutMethodConfigured}",
                organizationId, status.WelcomeGuideCompleted, showModal, organization.AgreementMethod, agreementUploaded, organization.SetupChecklistDismissed, squareConnected, payoutMethodConfigured);

            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SETUP] Error getting setup status for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("dismiss-checklist")]
    public async Task<ActionResult<object>> DismissSetupChecklist()
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[SETUP] Dismissing setup checklist for organization {OrganizationId}", organizationId);

        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                _logger.LogWarning("[SETUP] Organization {OrganizationId} not found during dismiss checklist operation", organizationId);
                return NotFound("Organization not found");
            }

            var previousStatus = organization.SetupChecklistDismissed;
            organization.SetupChecklistDismissed = true;
            organization.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("[SETUP] Setup checklist dismissed for organization {OrganizationId}: {PreviousStatus} -> {NewStatus}",
                organizationId, previousStatus, true);

            return Ok(new {
                success = true,
                setupChecklistDismissed = true,
                message = "Setup checklist dismissed successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SETUP] Error dismissing setup checklist for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("dismiss-welcome-guide")]
    public async Task<ActionResult<object>> DismissWelcomeGuide()
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[SETUP] Dismissing welcome guide for organization {OrganizationId}", organizationId);

        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                _logger.LogWarning("[SETUP] Organization {OrganizationId} not found during dismiss operation", organizationId);
                return NotFound("Organization not found");
            }

            var previousStatus = organization.WelcomeGuideCompleted;
            organization.WelcomeGuideCompleted = true;
            organization.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("[SETUP] Welcome guide dismissed for organization {OrganizationId}: {PreviousStatus} -> {NewStatus}",
                organizationId, previousStatus, true);

            return Ok(new {
                success = true,
                welcomeGuideCompleted = true,
                message = "Welcome guide dismissed successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SETUP] Error dismissing welcome guide for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("consignor-onboarding")]
    public async Task<ActionResult<object>> GetConsignorOnboarding()
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[CONSIGNOR_ONBOARDING] Getting consignor onboarding settings for organization {OrganizationId}", organizationId);

        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                _logger.LogWarning("[CONSIGNOR_ONBOARDING] Organization {OrganizationId} not found", organizationId);
                return NotFound("Organization not found");
            }

            ConsignorOnboardingDto onboardingSettings = null;
            if (!string.IsNullOrEmpty(organization.ConsignorSettings))
            {
                try
                {
                    onboardingSettings = JsonSerializer.Deserialize<ConsignorOnboardingDto>(organization.ConsignorSettings);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "[CONSIGNOR_ONBOARDING] Failed to parse existing consignor settings JSON for organization {OrganizationId}", organizationId);
                }
            }

            // Initialize with defaults if null
            onboardingSettings ??= CreateDefaultConsignorOnboardingSettings();

            _logger.LogInformation("[CONSIGNOR_ONBOARDING] Successfully retrieved consignor onboarding settings for organization {OrganizationId}", organizationId);
            return Ok(onboardingSettings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CONSIGNOR_ONBOARDING] Error getting consignor onboarding settings for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("consignor-onboarding")]
    public async Task<ActionResult<object>> UpdateConsignorOnboarding([FromBody] UpdateConsignorOnboardingRequest request)
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[CONSIGNOR_ONBOARDING] Updating consignor onboarding settings for organization {OrganizationId}", organizationId);

        if (request == null)
        {
            return BadRequest("Request body cannot be null");
        }

        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                _logger.LogWarning("[CONSIGNOR_ONBOARDING] Organization {OrganizationId} not found during update", organizationId);
                return NotFound("Organization not found");
            }

            // Get existing settings or create defaults
            ConsignorOnboardingDto currentSettings = null;
            if (!string.IsNullOrEmpty(organization.ConsignorSettings))
            {
                try
                {
                    currentSettings = JsonSerializer.Deserialize<ConsignorOnboardingDto>(organization.ConsignorSettings);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "[CONSIGNOR_ONBOARDING] Failed to parse existing consignor settings JSON for organization {OrganizationId}", organizationId);
                }
            }

            // Initialize with defaults if null
            currentSettings ??= CreateDefaultConsignorOnboardingSettings();

            // Apply partial updates
            if (request.AgreementRequirement.HasValue)
            {
                currentSettings.AgreementRequirement = request.AgreementRequirement.Value;
            }

            if (request.AgreementTemplateId.HasValue)
            {
                currentSettings.AgreementTemplateId = request.AgreementTemplateId.Value;
            }

            if (!string.IsNullOrEmpty(request.AcknowledgeTermsText))
            {
                currentSettings.AcknowledgeTermsText = request.AcknowledgeTermsText;
            }

            if (request.ApprovalMode.HasValue)
            {
                currentSettings.ApprovalMode = request.ApprovalMode.Value;
            }

            // Save updated settings
            organization.ConsignorSettings = JsonSerializer.Serialize(currentSettings);
            organization.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("[CONSIGNOR_ONBOARDING] Consignor onboarding settings updated for organization {OrganizationId}", organizationId);
            return Ok(new {
                success = true,
                message = "Consignor onboarding settings updated successfully",
                data = currentSettings
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CONSIGNOR_ONBOARDING] Error updating consignor onboarding settings for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    private ConsignorOnboardingDto CreateDefaultConsignorOnboardingSettings()
    {
        return new ConsignorOnboardingDto
        {
            AgreementRequirement = AgreementRequirement.Upload,
            AgreementTemplateId = null,
            AcknowledgeTermsText = "I acknowledge that I have read and agree to the terms and conditions.",
            ApprovalMode = ApprovalMode.Auto
        };
    }

    private Guid GetOrganizationId()
    {
        var organizationIdClaim = User.FindFirst("organizationId")?.Value;
        if (organizationIdClaim != null && Guid.TryParse(organizationIdClaim, out var organizationId))
        {
            return organizationId;
        }

        throw new UnauthorizedAccessException("Organization ID not found in token");
    }
}