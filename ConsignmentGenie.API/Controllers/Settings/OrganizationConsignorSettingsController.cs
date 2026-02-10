using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ConsignmentGenie.Infrastructure.Data;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Core.DTOs.Settings;
using ConsignmentGenie.Core.DTOs.Organization;
using ConsignmentGenie.Core.Enums;
using System.Security.Claims;
using System.Text.Json;

namespace ConsignmentGenie.API.Controllers.Settings;

[ApiController]
[Route("api/settings/consignor")]
[Authorize(Roles = "Owner,Clerk,Consignor")]
public class OrganizationConsignorSettingsController : ControllerBase
{
    private readonly ConsignmentGenieContext _context;
    private readonly ILogger<OrganizationConsignorSettingsController> _logger;

    public OrganizationConsignorSettingsController(ConsignmentGenieContext context, ILogger<OrganizationConsignorSettingsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GENERATE NUMBER - Get next available provider number
    [HttpGet("default-terms/generate-number")]
    [Obsolete("This endpoint is not used by the current UI and may be removed in a future version")]
    public async Task<ActionResult<ApiResponse<string>>> GenerateProviderNumber()
    {
        try
        {
            var organizationId = GetOrganizationId();
            var providerNumber = await GenerateProviderNumber(organizationId);
            return Ok(ApiResponse<string>.SuccessResult(providerNumber));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating provider number");
            return StatusCode(500, ApiResponse<string>.ErrorResult("Failed to generate provider number"));
        }
    }

    // GET STORE CODE - Get organization's provider registration store code
    [HttpGet("consignor-onboarding/store-code")]
    [Obsolete("This endpoint is not used by the current UI and may be removed in a future version")]
    public async Task<ActionResult<ApiResponse<StoreCodeDto>>> GetStoreCode()
    {
        try
        {
            var organizationId = GetOrganizationId();
            var organization = await _context.Organizations
                .Where(o => o.Id == organizationId)
                .FirstOrDefaultAsync();

            if (organization == null)
            {
                return NotFound(ApiResponse<StoreCodeDto>.ErrorResult("Organization not found"));
            }

            var storeCode = new StoreCodeDto
            {
                StoreCode = organization.StoreCode ?? "",
                IsEnabled = organization.StoreCodeEnabled,
                RegistrationUrl = $"{Request.Scheme}://{Request.Host}/provider-register/{organization.StoreCode}"
            };

            return Ok(ApiResponse<StoreCodeDto>.SuccessResult(storeCode));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting store code for organization {OrganizationId}", GetOrganizationId());
            return StatusCode(500, ApiResponse<StoreCodeDto>.ErrorResult("Failed to retrieve store code"));
        }
    }

    // TOGGLE STORE CODE - Enable/disable provider self-registration
    [HttpPost("consignor-onboarding/store-code/toggle")]
    [Authorize(Roles = "Owner")]
    [Obsolete("This endpoint is not used by the current UI and may be removed in a future version")]
    public async Task<ActionResult<ApiResponse<StoreCodeDto>>> ToggleStoreCode([FromBody] ToggleStoreCodeRequest request)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var userId = GetUserId();

            var organization = await _context.Organizations
                .Where(o => o.Id == organizationId)
                .FirstOrDefaultAsync();

            if (organization == null)
            {
                return NotFound(ApiResponse<StoreCodeDto>.ErrorResult("Organization not found"));
            }

            organization.StoreCodeEnabled = request.IsEnabled;
            // Note: Organization entity doesn't have UpdatedAt/UpdatedBy - these are on BaseEntity if available

            await _context.SaveChangesAsync();

            var storeCode = new StoreCodeDto
            {
                StoreCode = organization.StoreCode ?? "",
                IsEnabled = organization.StoreCodeEnabled,
                RegistrationUrl = $"{Request.Scheme}://{Request.Host}/provider-register/{organization.StoreCode}"
            };

            return Ok(ApiResponse<StoreCodeDto>.SuccessResult(storeCode));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling store code for organization {OrganizationId}", GetOrganizationId());
            return StatusCode(500, ApiResponse<StoreCodeDto>.ErrorResult("Failed to toggle store code"));
        }
    }

    // TOGGLE AGREEMENT REQUIREMENT - Enable/disable agreement requirement for item submission
    [HttpPost("consignor-agreement/agreement-requirement/toggle")]
    [Authorize(Roles = "Owner")]
    [Obsolete("This endpoint is not used by the current UI and may be removed in a future version")]
    public async Task<ActionResult<ApiResponse<ConsignorSettingsSummaryDto>>> ToggleAgreementRequirement([FromBody] ToggleAgreementRequirementRequest request)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var organization = await _context.Organizations
                .Where(o => o.Id == organizationId)
                .FirstOrDefaultAsync();

            if (organization == null)
            {
                return NotFound(ApiResponse<ConsignorSettingsSummaryDto>.ErrorResult("Organization not found"));
            }

            // Set AgreementMethod based on request
            organization.AgreementMethod = request.AgreementMethod;
            await _context.SaveChangesAsync();

            // Get updated settings to return
            var totalProviders = await _context.Consignors
                .Where(p => p.OrganizationId == organizationId)
                .CountAsync();

            var pendingRegistrations = await _context.Consignors
                .Where(p => p.OrganizationId == organizationId && p.Status == Core.Enums.ConsignorStatus.Pending)
                .CountAsync();

            var settings = new ConsignorSettingsSummaryDto
            {
                AllowSelfRegistration = organization.StoreCodeEnabled,
                RegistrationCode = organization.StoreCode ?? "",
                RegistrationUrl = $"{Request.Scheme}://{Request.Host}/provider-register/{organization.StoreCode}",
                TotalProviders = totalProviders,
                PendingRegistrations = pendingRegistrations,
                DefaultCommissionRate = organization.DefaultSplitPercentage / 100m,
                // NOTE: AutoSendAgreementOnRegistration removed - old email agreement system deprecated
                // RequireAgreementOnFile removed - use AgreementMethod instead
                AgreementMethod = organization.AgreementMethod
            };

            return Ok(ApiResponse<ConsignorSettingsSummaryDto>.SuccessResult(settings));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling agreement requirement for organization {OrganizationId}", GetOrganizationId());
            return StatusCode(500, ApiResponse<ConsignorSettingsSummaryDto>.ErrorResult("Failed to toggle agreement requirement"));
        }
    }

    // NOTE: Auto-send agreement endpoint removed - old email agreement system deprecated

    // REGENERATE STORE CODE - Generate new store code
    [HttpPost("consignor-onboarding/store-code/regenerate")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<ApiResponse<StoreCodeDto>>> RegenerateStoreCode()
    {
        try
        {
            var organizationId = GetOrganizationId();
            var userId = GetUserId();

            var organization = await _context.Organizations
                .Where(o => o.Id == organizationId)
                .FirstOrDefaultAsync();

            if (organization == null)
            {
                return NotFound(ApiResponse<StoreCodeDto>.ErrorResult("Organization not found"));
            }

            // Generate new unique store code
            string newCode;
            bool codeExists;
            do
            {
                newCode = GenerateRandomCode(8);
                codeExists = await _context.Organizations
                    .AnyAsync(o => o.StoreCode == newCode);
            } while (codeExists);

            organization.StoreCode = newCode;
            // Note: Organization entity doesn't have UpdatedAt/UpdatedBy - these are on BaseEntity if available

            await _context.SaveChangesAsync();

            var storeCode = new StoreCodeDto
            {
                StoreCode = organization.StoreCode ?? "",
                IsEnabled = organization.StoreCodeEnabled,
                RegistrationUrl = $"{Request.Scheme}://{Request.Host}/provider-register/{organization.StoreCode}"
            };

            return Ok(ApiResponse<StoreCodeDto>.SuccessResult(storeCode));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error regenerating store code for organization {OrganizationId}", GetOrganizationId());
            return StatusCode(500, ApiResponse<StoreCodeDto>.ErrorResult("Failed to regenerate store code"));
        }
    }

    // GET SETTINGS SUMMARY - Get overview of provider-related settings
    [HttpGet("default-permissions")]
    [Obsolete("This endpoint is not used by the current UI and may be removed in a future version")]
    public async Task<ActionResult<ApiResponse<ConsignorSettingsSummaryDto>>> GetSettingsSummary()
    {
        try
        {
            var organizationId = GetOrganizationId();

            var organization = await _context.Organizations
                .Where(o => o.Id == organizationId)
                .FirstOrDefaultAsync();

            if (organization == null)
            {
                return NotFound(ApiResponse<ConsignorSettingsSummaryDto>.ErrorResult("Organization not found"));
            }

            // Get provider stats for context
            var totalProviders = await _context.Consignors
                .Where(p => p.OrganizationId == organizationId)
                .CountAsync();

            var pendingRegistrations = await _context.Consignors
                .Where(p => p.OrganizationId == organizationId && p.Status == Core.Enums.ConsignorStatus.Pending)
                .CountAsync();

            var settings = new ConsignorSettingsSummaryDto
            {
                AllowSelfRegistration = organization.StoreCodeEnabled,
                RegistrationCode = organization.StoreCode ?? "",
                RegistrationUrl = $"{Request.Scheme}://{Request.Host}/provider-register/{organization.StoreCode}",
                TotalProviders = totalProviders,
                PendingRegistrations = pendingRegistrations,
                DefaultCommissionRate = organization.DefaultSplitPercentage / 100m, // Convert percentage to decimal
                // NOTE: AutoSendAgreementOnRegistration removed - old email agreement system deprecated
                // RequireAgreementOnFile removed - use AgreementMethod instead
                AgreementMethod = organization.AgreementMethod
            };

            return Ok(ApiResponse<ConsignorSettingsSummaryDto>.SuccessResult(settings));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting settings summary for organization {OrganizationId}", GetOrganizationId());
            return StatusCode(500, ApiResponse<ConsignorSettingsSummaryDto>.ErrorResult("Failed to retrieve settings summary"));
        }
    }

    #region Private Helper Methods

    private async Task<string> GenerateProviderNumber(Guid organizationId)
    {
        var lastNumber = await _context.Consignors
            .Where(p => p.OrganizationId == organizationId)
            .OrderByDescending(p => p.ConsignorNumber)
            .Select(p => p.ConsignorNumber)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (lastNumber != null)
        {
            var parts = lastNumber.Split('-');
            if (parts.Length > 1 && int.TryParse(parts[1], out int num))
            {
                nextNumber = num + 1;
            }
        }

        return $"PRV-{nextNumber:D5}";
    }

    private static string GenerateRandomCode(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    private Guid GetOrganizationId()
    {
        var orgIdClaim = User.FindFirst("organizationId")?.Value;
        return orgIdClaim != null ? Guid.Parse(orgIdClaim) : Guid.Empty;
    }

    private Guid GetUserId()
    {
        // Try both standard claim types
        var userIdClaim = User.FindFirst("userId")?.Value;
        var nameIdentifierClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var finalValue = userIdClaim ?? nameIdentifierClaim;
        if (Guid.TryParse(finalValue, out var userId))
            return userId;
        throw new UnauthorizedAccessException("User ID not found in token");
    }

    #endregion

    // =========================
    // CONSIGNOR ONBOARDING (moved from SettingsController)
    // =========================

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

            // Parse existing ConsignorSettings or create default
            ConsignorOnboardingDto settings;
            if (!string.IsNullOrEmpty(organization.ConsignorSettings))
            {
                try
                {
                    var fullSettings = JsonSerializer.Deserialize<Dictionary<string, object>>(organization.ConsignorSettings);
                    if (fullSettings?.ContainsKey("onboarding") == true)
                    {
                        var onboardingJson = JsonSerializer.Serialize(fullSettings["onboarding"]);
                        settings = JsonSerializer.Deserialize<ConsignorOnboardingDto>(onboardingJson) ?? CreateDefaultConsignorOnboardingSettings();
                    }
                    else
                    {
                        settings = CreateDefaultConsignorOnboardingSettings();
                    }
                    _logger.LogDebug("[CONSIGNOR_ONBOARDING] Loaded existing consignor onboarding settings for organization {OrganizationId}", organizationId);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "[CONSIGNOR_ONBOARDING] Failed to parse consignor settings JSON for organization {OrganizationId}, using defaults", organizationId);
                    settings = CreateDefaultConsignorOnboardingSettings();
                }
            }
            else
            {
                settings = CreateDefaultConsignorOnboardingSettings();
                _logger.LogDebug("[CONSIGNOR_ONBOARDING] Using default consignor onboarding settings for organization {OrganizationId}", organizationId);
            }

            // Sync with Organization entity fields for backwards compatibility
            if (!string.IsNullOrEmpty(organization.AgreementMethod))
            {
                settings.AgreementRequirement = organization.AgreementMethod.ToLower() switch
                {
                    "acknowledge" => AgreementRequirement.Acknowledge,
                    "upload" => AgreementRequirement.Upload,
                    _ => AgreementRequirement.Upload
                };
            }

            if (!string.IsNullOrEmpty(organization.AgreementTemplateCloudinaryUrl))
            {
                settings.AgreementTemplateId = Guid.NewGuid(); // Generate a temporary GUID for backwards compatibility with UI
            }

            if (!string.IsNullOrEmpty(organization.AcknowledgeTermsText))
            {
                settings.AcknowledgeTermsText = organization.AcknowledgeTermsText;
            }

            settings.ApprovalMode = organization.ApprovalMode;

            return Ok(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CONSIGNOR_ONBOARDING] Error getting consignor onboarding settings for organization {OrganizationId}", organizationId);
            return StatusCode(500, "An error occurred while retrieving consignor onboarding settings");
        }
    }

    [HttpPut("consignor-onboarding")]
    [HttpPatch("consignor-onboarding")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<object>> UpdateConsignorOnboarding([FromBody] UpdateConsignorOnboardingRequest request)
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[CONSIGNOR_ONBOARDING] Updating consignor onboarding settings for organization {OrganizationId}", organizationId);

        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                _logger.LogWarning("[CONSIGNOR_ONBOARDING] Organization {OrganizationId} not found", organizationId);
                return NotFound("Organization not found");
            }

            // Parse existing ConsignorSettings or create new structure
            Dictionary<string, object> fullSettings;
            if (!string.IsNullOrEmpty(organization.ConsignorSettings))
            {
                try
                {
                    fullSettings = JsonSerializer.Deserialize<Dictionary<string, object>>(organization.ConsignorSettings) ?? new Dictionary<string, object>();
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "[CONSIGNOR_ONBOARDING] Failed to parse consignor settings JSON for organization {OrganizationId}, creating new structure", organizationId);
                    fullSettings = new Dictionary<string, object>();
                }
            }
            else
            {
                fullSettings = new Dictionary<string, object>();
            }

            // Get current onboarding settings or create default
            var currentSettings = CreateDefaultConsignorOnboardingSettings();
            if (fullSettings.ContainsKey("onboarding"))
            {
                try
                {
                    var onboardingJson = JsonSerializer.Serialize(fullSettings["onboarding"]);
                    currentSettings = JsonSerializer.Deserialize<ConsignorOnboardingDto>(onboardingJson) ?? currentSettings;
                }
                catch (JsonException)
                {
                    // Use defaults if parsing fails
                }
            }

            // Apply updates
            if (request.AgreementRequirement.HasValue) currentSettings.AgreementRequirement = request.AgreementRequirement.Value;
            if (request.AgreementTemplateId.HasValue) currentSettings.AgreementTemplateId = request.AgreementTemplateId;
            if (request.AcknowledgeTermsText != null) currentSettings.AcknowledgeTermsText = request.AcknowledgeTermsText;
            if (request.ApprovalMode.HasValue) currentSettings.ApprovalMode = request.ApprovalMode.Value;

            currentSettings.LastUpdated = DateTime.UtcNow;

            // Update the full settings structure
            fullSettings["onboarding"] = currentSettings;

            // Serialize and save
            organization.ConsignorSettings = JsonSerializer.Serialize(fullSettings);

            // Sync with Organization entity fields for backwards compatibility
            organization.AgreementMethod = currentSettings.AgreementRequirement switch
            {
                AgreementRequirement.Acknowledge => "acknowledge",
                AgreementRequirement.Upload => "upload",
                _ => "none"
            };
            // Note: AgreementTemplateId is now handled through the OrganizationAgreementController upload endpoint
            organization.AcknowledgeTermsText = currentSettings.AcknowledgeTermsText;
            organization.ApprovalMode = currentSettings.ApprovalMode;

            await _context.SaveChangesAsync();

            _logger.LogInformation("[CONSIGNOR_ONBOARDING] Successfully updated consignor onboarding settings for organization {OrganizationId}", organizationId);
            return Ok(new { success = true, data = currentSettings, message = "Consignor onboarding settings updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CONSIGNOR_ONBOARDING] Error updating consignor onboarding settings for organization {OrganizationId}", organizationId);
            return StatusCode(500, "An error occurred while updating consignor onboarding settings");
        }
    }

    // =========================
    // CONSIGNOR DEFAULT TERMS (moved from SettingsController)
    // =========================

    [HttpGet("default-terms")]
    public async Task<ActionResult<ConsignorDefaultsDto>> GetConsignorDefaults()
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[CONSIGNOR_DEFAULTS] Getting default consignor terms for organization {OrganizationId}", organizationId);

        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                _logger.LogWarning("[CONSIGNOR_DEFAULTS] Organization {OrganizationId} not found", organizationId);
                return NotFound("Organization not found");
            }

            ConsignorDefaultsDto defaults;

            if (!string.IsNullOrEmpty(organization.ConsignorSettings))
            {
                try
                {
                    var consignorSettings = JsonSerializer.Deserialize<Dictionary<string, object>>(organization.ConsignorSettings);

                    if (consignorSettings != null && consignorSettings.ContainsKey("defaults"))
                    {
                        var defaultsJson = consignorSettings["defaults"].ToString();
                        defaults = JsonSerializer.Deserialize<ConsignorDefaultsDto>(defaultsJson!) ?? CreateDefaultConsignorDefaults(organization);
                    }
                    else
                    {
                        defaults = CreateDefaultConsignorDefaults(organization);
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "[CONSIGNOR_DEFAULTS] Failed to deserialize ConsignorSettings for organization {OrganizationId}, using defaults", organizationId);
                    defaults = CreateDefaultConsignorDefaults(organization);
                }
            }
            else
            {
                defaults = CreateDefaultConsignorDefaults(organization);
            }

            return Ok(defaults);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CONSIGNOR_DEFAULTS] Error getting consignor defaults for organization {OrganizationId}", organizationId);
            return StatusCode(500, "An error occurred while retrieving consignor default terms");
        }
    }

    [HttpPut("default-terms")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult> UpdateConsignorDefaults([FromBody] ConsignorDefaultsDto defaultsDto)
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[CONSIGNOR_DEFAULTS] Updating default consignor terms for organization {OrganizationId}", organizationId);

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                _logger.LogWarning("[CONSIGNOR_DEFAULTS] Organization {OrganizationId} not found", organizationId);
                return NotFound("Organization not found");
            }

            // Update the defaults timestamp
            defaultsDto.LastUpdated = DateTime.UtcNow;

            // Get or create the consignor settings structure
            Dictionary<string, object> consignorSettings;

            if (!string.IsNullOrEmpty(organization.ConsignorSettings))
            {
                try
                {
                    consignorSettings = JsonSerializer.Deserialize<Dictionary<string, object>>(organization.ConsignorSettings)
                                     ?? new Dictionary<string, object>();
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "[CONSIGNOR_DEFAULTS] Failed to deserialize ConsignorSettings for organization {OrganizationId}, creating new", organizationId);
                    consignorSettings = new Dictionary<string, object>();
                }
            }
            else
            {
                consignorSettings = new Dictionary<string, object>();
            }

            // Update the defaults section
            consignorSettings["defaults"] = defaultsDto;

            // Also update the legacy DefaultSplitPercentage field for backward compatibility
            organization.DefaultSplitPercentage = 100M - defaultsDto.ShopCommissionPercent;

            // Serialize and save
            organization.ConsignorSettings = JsonSerializer.Serialize(consignorSettings);

            await _context.SaveChangesAsync();

            _logger.LogInformation("[CONSIGNOR_DEFAULTS] Successfully updated consignor defaults for organization {OrganizationId}", organizationId);
            return Ok(new { success = true, data = defaultsDto, message = "Consignor default terms updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CONSIGNOR_DEFAULTS] Error updating consignor defaults for organization {OrganizationId}", organizationId);
            return StatusCode(500, "An error occurred while updating consignor default terms");
        }
    }

    private static ConsignorOnboardingDto CreateDefaultConsignorOnboardingSettings()
    {
        return new ConsignorOnboardingDto
        {
            AgreementRequirement = AgreementRequirement.Upload,
            AgreementTemplateId = null,
            AcknowledgeTermsText = null,
            ApprovalMode = ApprovalMode.Manual,
            LastUpdated = DateTime.UtcNow
        };
    }


    private static ConsignorDefaultsDto CreateDefaultConsignorDefaults(Organization organization)
    {
        return new ConsignorDefaultsDto
        {
            ShopCommissionPercent = 100M - organization.DefaultSplitPercentage,  // Convert consignor percentage to shop percentage
            ConsignmentPeriodDays = 90,
            RetrievalPeriodDays = 14,
            UnsoldItemPolicy = "return-to-consignor",
            LastUpdated = DateTime.UtcNow
        };
    }
}