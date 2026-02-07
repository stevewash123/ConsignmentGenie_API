using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ConsignmentGenie.Core.DTOs.Settings;
using ConsignmentGenie.Core.DTOs.Organization;
using System.Text.Json;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/organizations/business")]
[Authorize(Roles = "Owner")]
public class OrganizationBusinessController : ControllerBase
{
    private readonly ConsignmentGenieContext _context;
    private readonly ILogger<OrganizationBusinessController> _logger;

    public OrganizationBusinessController(ConsignmentGenieContext context, ILogger<OrganizationBusinessController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("settings")]
    public async Task<ActionResult<BusinessSettingsDto>> GetBusinessSettings()
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[BUSINESS_SETTINGS] Getting business settings for organization {OrganizationId}", organizationId);

        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                _logger.LogWarning("[BUSINESS_SETTINGS] Organization {OrganizationId} not found", organizationId);
                return NotFound("Organization not found");
            }

            // Parse JSON settings or create defaults
            BusinessSettingsDto? businessSettings = null;
            if (!string.IsNullOrEmpty(organization.BusinessSettings))
            {
                try
                {
                    businessSettings = JsonSerializer.Deserialize<BusinessSettingsDto>(organization.BusinessSettings);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "[BUSINESS_SETTINGS] Failed to parse business settings JSON for organization {OrganizationId}", organizationId);
                }
            }

            // Create defaults if parsing failed or no settings exist
            businessSettings ??= new BusinessSettingsDto
            {
                Commission = new CommissionDto
                {
                    DefaultSplit = organization.DefaultSplitPercentage == 60 ? "60/40" : "70/30",
                    AllowCustomSplitsPerConsignor = false,
                    AllowCustomSplitsPerItem = false
                },
                Tax = new TaxDto
                {
                    SalesTaxRate = organization.TaxRate * 100, // Convert from decimal to percentage
                    TaxIncludedInPrices = false,
                    ChargeTaxOnShipping = false
                },
                Payouts = new PayoutDto
                {
                    Schedule = "monthly",
                    MinimumAmount = 25.00m,
                    HoldPeriodDays = 14
                },
                Items = new ItemPolicyDto
                {
                    DefaultConsignmentPeriodDays = 90,
                    EnableAutoMarkdowns = false,
                    MarkdownSchedule = new MarkdownScheduleDto
                    {
                        After30Days = 0,
                        After60Days = 0,
                        After90DaysAction = "return"
                    }
                }
            };

            _logger.LogDebug("[BUSINESS_SETTINGS] Business settings retrieved for organization {OrganizationId}", organizationId);
            return Ok(businessSettings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BUSINESS_SETTINGS] Error getting business settings for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("settings")]
    public async Task<ActionResult<object>> UpdateBusinessSettings([FromBody] BusinessSettingsDto businessSettingsDto)
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[BUSINESS_SETTINGS] Updating business settings for organization {OrganizationId}", organizationId);

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
                _logger.LogWarning("[BUSINESS_SETTINGS] Organization {OrganizationId} not found during update", organizationId);
                return NotFound("Organization not found");
            }

            // Update basic fields that exist in Organization entity
            if (businessSettingsDto.Commission != null)
            {
                // Parse commission split and update organization field
                var splitParts = businessSettingsDto.Commission.DefaultSplit.Split('/');
                if (splitParts.Length == 2 && decimal.TryParse(splitParts[0], out var consignorPercentage))
                {
                    organization.DefaultSplitPercentage = consignorPercentage;
                }
            }

            if (businessSettingsDto.Tax != null)
            {
                organization.TaxRate = businessSettingsDto.Tax.SalesTaxRate / 100; // Convert percentage to decimal
            }

            // Store full settings as JSON
            organization.BusinessSettings = JsonSerializer.Serialize(businessSettingsDto);
            organization.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("[BUSINESS_SETTINGS] Business settings updated for organization {OrganizationId}", organizationId);

            return Ok(new {
                success = true,
                message = "Business settings updated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BUSINESS_SETTINGS] Error updating business settings for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPatch("settings")]
    public async Task<ActionResult<BusinessSettingsDto>> UpdateBusinessSettingsPartial([FromBody] UpdateBusinessSettingsRequest request)
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[BUSINESS_SETTINGS] Updating business settings (partial) for organization {OrganizationId}", organizationId);

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
                _logger.LogWarning("[BUSINESS_SETTINGS] Organization {OrganizationId} not found during partial update", organizationId);
                return NotFound("Organization not found");
            }

            // Parse current business settings or create defaults
            BusinessSettingsDto currentSettings;
            if (!string.IsNullOrEmpty(organization.BusinessSettings))
            {
                try
                {
                    currentSettings = JsonSerializer.Deserialize<BusinessSettingsDto>(organization.BusinessSettings) ?? new();
                }
                catch
                {
                    currentSettings = new();
                }
            }
            else
            {
                currentSettings = new();
            }

            // Apply partial updates - only update non-null values
            if (request.DefaultSplit != null)
            {
                currentSettings.Commission.DefaultSplit = request.DefaultSplit;

                // Also update the Organization entity field
                var splitParts = request.DefaultSplit.Split('/');
                if (splitParts.Length == 2 && decimal.TryParse(splitParts[0], out var consignorPercentage))
                {
                    organization.DefaultSplitPercentage = consignorPercentage;
                }
            }

            if (request.AllowCustomSplitsPerConsignor.HasValue)
                currentSettings.Commission.AllowCustomSplitsPerConsignor = request.AllowCustomSplitsPerConsignor.Value;

            if (request.AllowCustomSplitsPerItem.HasValue)
                currentSettings.Commission.AllowCustomSplitsPerItem = request.AllowCustomSplitsPerItem.Value;

            if (request.SalesTaxRate.HasValue)
            {
                currentSettings.Tax.SalesTaxRate = request.SalesTaxRate.Value;
                organization.TaxRate = request.SalesTaxRate.Value; // Update Organization field
            }

            if (request.TaxIncludedInPrices.HasValue)
                currentSettings.Tax.TaxIncludedInPrices = request.TaxIncludedInPrices.Value;

            if (request.ChargeTaxOnShipping.HasValue)
                currentSettings.Tax.ChargeTaxOnShipping = request.ChargeTaxOnShipping.Value;

            if (request.TaxIdEin != null)
                currentSettings.Tax.TaxIdEin = request.TaxIdEin;

            if (request.HoldPeriodDays.HasValue)
                currentSettings.Payouts.HoldPeriodDays = request.HoldPeriodDays.Value;

            if (request.MinimumAmount.HasValue)
                currentSettings.Payouts.MinimumAmount = request.MinimumAmount.Value;

            if (request.PayoutMethod != null)
                currentSettings.Payouts.Method = request.PayoutMethod;

            if (request.PayoutSchedule != null)
                currentSettings.Payouts.Schedule = request.PayoutSchedule;

            if (request.AutoProcessing.HasValue)
                currentSettings.Payouts.AutoProcessing = request.AutoProcessing.Value;

            if (request.RefundPolicy != null)
                currentSettings.Payouts.RefundPolicy = request.RefundPolicy;

            if (request.RefundWindowDays.HasValue)
                currentSettings.Payouts.RefundWindowDays = request.RefundWindowDays.Value;

            if (request.DefaultConsignmentPeriodDays.HasValue)
                currentSettings.Items.DefaultConsignmentPeriodDays = request.DefaultConsignmentPeriodDays.Value;

            if (request.EnableAutoMarkdowns.HasValue)
                currentSettings.Items.EnableAutoMarkdowns = request.EnableAutoMarkdowns.Value;

            if (request.ItemSubmissionMode != null)
                currentSettings.Items.ItemSubmissionMode = request.ItemSubmissionMode;

            if (request.AutoApproveItems.HasValue)
                currentSettings.Items.AutoApproveItems = request.AutoApproveItems.Value;

            // Serialize updated settings
            organization.BusinessSettings = JsonSerializer.Serialize(currentSettings);
            organization.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("[BUSINESS_SETTINGS] Business settings partially updated for organization {OrganizationId}", organizationId);

            return Ok(new { success = true, data = currentSettings });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BUSINESS_SETTINGS] Error partially updating business settings for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("profile")]
    public async Task<ActionResult<ShopProfileDto>> GetProfile()
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[PROFILE] Getting profile for organization {OrganizationId}", organizationId);

        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                _logger.LogWarning("[PROFILE] Organization {OrganizationId} not found", organizationId);
                return NotFound("Organization not found");
            }

            var profile = new ShopProfileDto
            {
                ShopName = organization.Name,
                ShopDescription = organization.ShopDescription,
                ShopLogoUrl = organization.ShopLogoUrl,
                ShopBannerUrl = organization.ShopBannerUrl,
                ShopPhone = organization.ShopPhone,
                ShopEmail = organization.ShopEmail,
                ShopWebsite = organization.ShopWebsite,
                ShopAddress1 = organization.ShopAddress1,
                ShopAddress2 = organization.ShopAddress2,
                ShopCity = organization.ShopCity,
                ShopState = organization.ShopState,
                ShopZip = organization.ShopZip,
                ShopCountry = organization.ShopCountry,
                ShopTimezone = organization.ShopTimezone,
                TaxRate = organization.TaxRate
            };

            _logger.LogDebug("[PROFILE] Profile retrieved for organization {OrganizationId}: ShopName={ShopName}",
                organizationId, profile.ShopName);

            return Ok(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PROFILE] Error getting profile for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("profile")]
    public async Task<ActionResult> UpdateProfile([FromBody] ShopProfileDto profileDto)
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[PROFILE] Updating profile for organization {OrganizationId}", organizationId);

        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                _logger.LogWarning("[PROFILE] Organization {OrganizationId} not found", organizationId);
                return NotFound("Organization not found");
            }

            organization.Name = profileDto.ShopName;
            organization.ShopDescription = profileDto.ShopDescription;
            organization.ShopLogoUrl = profileDto.ShopLogoUrl;
            organization.ShopBannerUrl = profileDto.ShopBannerUrl;
            organization.ShopPhone = profileDto.ShopPhone;
            organization.ShopEmail = profileDto.ShopEmail;
            organization.ShopWebsite = profileDto.ShopWebsite;
            organization.ShopAddress1 = profileDto.ShopAddress1;
            organization.ShopAddress2 = profileDto.ShopAddress2;
            organization.ShopCity = profileDto.ShopCity;
            organization.ShopState = profileDto.ShopState;
            organization.ShopZip = profileDto.ShopZip;
            organization.ShopCountry = profileDto.ShopCountry;
            organization.ShopTimezone = profileDto.ShopTimezone;
            organization.TaxRate = profileDto.TaxRate ?? 0;

            await _context.SaveChangesAsync();

            _logger.LogInformation("[PROFILE] Profile updated successfully for organization {OrganizationId}: ShopName={ShopName}",
                organizationId, profileDto.ShopName);

            return Ok(new { success = true, message = "Profile updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PROFILE] Error updating profile for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPatch("profile")]
    public async Task<ActionResult<ShopProfileDto>> UpdateProfileSettings([FromBody] UpdateShopProfileRequest request)
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[PROFILE] Updating profile settings for organization {OrganizationId}", organizationId);

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
                _logger.LogWarning("[PROFILE] Organization {OrganizationId} not found during profile update", organizationId);
                return NotFound("Organization not found");
            }

            if (request.ShopName != null)
                organization.Name = request.ShopName;

            if (request.ShopDescription != null)
                organization.ShopDescription = request.ShopDescription;

            if (request.ShopLogoUrl != null)
                organization.ShopLogoUrl = request.ShopLogoUrl;

            if (request.ShopBannerUrl != null)
                organization.ShopBannerUrl = request.ShopBannerUrl;

            if (request.ShopPhone != null)
                organization.ShopPhone = request.ShopPhone;

            if (request.ShopEmail != null)
                organization.ShopEmail = request.ShopEmail;

            if (request.ShopWebsite != null)
                organization.ShopWebsite = request.ShopWebsite;

            if (request.ShopAddress1 != null)
                organization.ShopAddress1 = request.ShopAddress1;

            if (request.ShopAddress2 != null)
                organization.ShopAddress2 = request.ShopAddress2;

            if (request.ShopCity != null)
                organization.ShopCity = request.ShopCity;

            if (request.ShopState != null)
                organization.ShopState = request.ShopState;

            if (request.ShopZip != null)
                organization.ShopZip = request.ShopZip;

            if (request.ShopCountry != null)
                organization.ShopCountry = request.ShopCountry;

            if (request.ShopTimezone != null)
                organization.ShopTimezone = request.ShopTimezone;

            if (request.TaxRate.HasValue)
                organization.TaxRate = request.TaxRate.Value;

            organization.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("[PROFILE] Profile settings updated for organization {OrganizationId}", organizationId);

            var updatedProfile = new ShopProfileDto
            {
                ShopName = organization.Name,
                ShopDescription = organization.ShopDescription,
                ShopLogoUrl = organization.ShopLogoUrl,
                ShopBannerUrl = organization.ShopBannerUrl,
                ShopPhone = organization.ShopPhone,
                ShopEmail = organization.ShopEmail,
                ShopWebsite = organization.ShopWebsite,
                ShopAddress1 = organization.ShopAddress1,
                ShopAddress2 = organization.ShopAddress2,
                ShopCity = organization.ShopCity,
                ShopState = organization.ShopState,
                ShopZip = organization.ShopZip,
                ShopCountry = organization.ShopCountry,
                ShopTimezone = organization.ShopTimezone,
                TaxRate = organization.TaxRate
            };

            return Ok(new { success = true, data = updatedProfile });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PROFILE] Error updating profile settings for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
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