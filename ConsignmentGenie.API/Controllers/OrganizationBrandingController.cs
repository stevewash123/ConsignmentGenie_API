using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ConsignmentGenie.Core.DTOs.Organization;
using ConsignmentGenie.Application.Services;
using System.Text.Json;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/organizations/branding")]
[Authorize(Roles = "Owner")]
public class OrganizationBrandingController : ControllerBase
{
    private readonly ConsignmentGenieContext _context;
    private readonly ILogger<OrganizationBrandingController> _logger;
    private readonly CloudinaryPhotoService _cloudinaryPhotoService;

    public OrganizationBrandingController(
        ConsignmentGenieContext context,
        ILogger<OrganizationBrandingController> logger,
        CloudinaryPhotoService cloudinaryPhotoService)
    {
        _context = context;
        _logger = logger;
        _cloudinaryPhotoService = cloudinaryPhotoService;
    }

    [HttpGet("settings")]
    public async Task<ActionResult<BrandingDto>> GetBranding()
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[BRANDING] Getting branding settings for organization {OrganizationId}", organizationId);

        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                _logger.LogWarning("[BRANDING] Organization {OrganizationId} not found", organizationId);
                return NotFound("Organization not found");
            }

            BrandingDto brandingSettings = null;
            if (!string.IsNullOrEmpty(organization.BrandingSettings))
            {
                try
                {
                    brandingSettings = JsonSerializer.Deserialize<BrandingDto>(organization.BrandingSettings);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "[BRANDING] Failed to parse existing branding settings JSON for organization {OrganizationId}", organizationId);
                }
            }

            // Initialize with defaults if null
            brandingSettings ??= CreateDefaultBrandingSettings();

            _logger.LogInformation("[BRANDING] Successfully retrieved branding settings for organization {OrganizationId}", organizationId);
            return Ok(brandingSettings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BRANDING] Error getting branding settings for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("settings")]
    public async Task<ActionResult<object>> UpdateBranding([FromBody] UpdateBrandingRequest request)
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[BRANDING] Updating branding settings for organization {OrganizationId}", organizationId);

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
                _logger.LogWarning("[BRANDING] Organization {OrganizationId} not found during update", organizationId);
                return NotFound("Organization not found");
            }

            // Get existing settings or create defaults
            BrandingDto currentSettings = null;
            if (!string.IsNullOrEmpty(organization.BrandingSettings))
            {
                try
                {
                    currentSettings = JsonSerializer.Deserialize<BrandingDto>(organization.BrandingSettings);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "[BRANDING] Failed to parse existing branding settings JSON for organization {OrganizationId}", organizationId);
                }
            }

            // Initialize with defaults if null
            currentSettings ??= CreateDefaultBrandingSettings();

            // Apply partial updates
            if (request.Logo != null)
            {
                currentSettings.Logo = request.Logo;
            }

            if (request.Colors != null)
            {
                currentSettings.Colors = request.Colors;
            }

            if (request.Typography != null)
            {
                currentSettings.Typography = request.Typography;
            }

            if (request.Style != null)
            {
                currentSettings.Style = request.Style;
            }

            // Update timestamp
            currentSettings.LastUpdated = DateTime.UtcNow;

            // Update organization entity fields
            if (currentSettings.Logo != null)
            {
                organization.ShopLogoUrl = currentSettings.Logo.Url;
            }

            // Store updated settings as JSON
            organization.BrandingSettings = JsonSerializer.Serialize(currentSettings);

            await _context.SaveChangesAsync();

            _logger.LogInformation("[BRANDING] Successfully updated branding settings for organization {OrganizationId}", organizationId);
            return Ok(new { success = true, data = currentSettings });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BRANDING] Error updating branding settings for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("logo")]
    public async Task<ActionResult<object>> UploadLogo(IFormFile logo)
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[BRANDING_LOGO] Uploading logo for organization {OrganizationId}", organizationId);

        if (logo == null || logo.Length == 0)
        {
            return BadRequest("No logo file provided");
        }

        try
        {
            // Upload to Cloudinary
            using var fileStream = logo.OpenReadStream();
            var (logoUrl, width, height) = await _cloudinaryPhotoService.UploadLogoAsync(organizationId, fileStream, logo.FileName);

            // Update branding settings in database
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                _logger.LogWarning("[BRANDING_LOGO] Organization {OrganizationId} not found", organizationId);
                return NotFound("Organization not found");
            }

            // Parse existing branding settings or create new
            BrandingDto currentSettings;
            if (!string.IsNullOrEmpty(organization.BrandingSettings))
            {
                try
                {
                    currentSettings = JsonSerializer.Deserialize<BrandingDto>(organization.BrandingSettings) ?? CreateDefaultBrandingSettings();
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "[BRANDING_LOGO] Failed to parse existing branding settings JSON for organization {OrganizationId}, using defaults", organizationId);
                    currentSettings = CreateDefaultBrandingSettings();
                }
            }
            else
            {
                currentSettings = CreateDefaultBrandingSettings();
            }

            // Update logo information
            currentSettings.Logo.Url = logoUrl;
            currentSettings.Logo.FileName = logo.FileName;
            currentSettings.Logo.UploadedAt = DateTime.UtcNow;
            currentSettings.Logo.Dimensions = new LogoDimensionsDto
            {
                Width = width,
                Height = height
            };
            currentSettings.LastUpdated = DateTime.UtcNow;

            // Save to database
            organization.BrandingSettings = JsonSerializer.Serialize(currentSettings);
            organization.ShopLogoUrl = logoUrl; // Also update the legacy field for backward compatibility
            await _context.SaveChangesAsync();

            // Create response
            var response = new LogoUploadResponse
            {
                Url = logoUrl,
                Dimensions = new LogoDimensionsDto
                {
                    Width = width,
                    Height = height
                }
            };

            _logger.LogInformation("[BRANDING_LOGO] Successfully uploaded logo for organization {OrganizationId}: {LogoUrl}", organizationId, logoUrl);
            return Ok(new { success = true, data = response });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("[BRANDING_LOGO] Invalid logo upload for organization {OrganizationId}: {Error}", organizationId, ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BRANDING_LOGO] Error uploading logo for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("logo")]
    public async Task<ActionResult<object>> RemoveLogo()
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[BRANDING_LOGO] Removing logo for organization {OrganizationId}", organizationId);

        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                _logger.LogWarning("[BRANDING_LOGO] Organization {OrganizationId} not found", organizationId);
                return NotFound("Organization not found");
            }

            // Get existing branding settings
            BrandingDto currentSettings = null;
            if (!string.IsNullOrEmpty(organization.BrandingSettings))
            {
                try
                {
                    currentSettings = JsonSerializer.Deserialize<BrandingDto>(organization.BrandingSettings);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "[BRANDING_LOGO] Failed to parse existing branding settings JSON for organization {OrganizationId}", organizationId);
                }
            }

            currentSettings ??= CreateDefaultBrandingSettings();

            // Delete logo from Cloudinary if it exists
            if (!string.IsNullOrEmpty(currentSettings.Logo?.Url))
            {
                try
                {
                    await _cloudinaryPhotoService.DeleteLogoAsync(currentSettings.Logo.Url);
                    _logger.LogInformation("[BRANDING_LOGO] Logo deleted from Cloudinary for organization {OrganizationId}", organizationId);
                }
                catch (Exception cloudinaryEx)
                {
                    _logger.LogWarning(cloudinaryEx, "[BRANDING_LOGO] Failed to delete logo from Cloudinary for organization {OrganizationId}, continuing with database update", organizationId);
                }
            }

            // Clear logo settings
            currentSettings.Logo.Url = null;
            currentSettings.Logo.FileName = null;
            currentSettings.Logo.UploadedAt = null;
            currentSettings.Logo.Dimensions = new LogoDimensionsDto { Width = 0, Height = 0 };
            currentSettings.LastUpdated = DateTime.UtcNow;

            // Update organization entity
            organization.ShopLogoUrl = null;
            organization.BrandingSettings = JsonSerializer.Serialize(currentSettings);

            await _context.SaveChangesAsync();

            _logger.LogInformation("[BRANDING_LOGO] Successfully removed logo for organization {OrganizationId}", organizationId);
            return Ok(new { success = true, message = "Logo removed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BRANDING_LOGO] Error removing logo for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    private BrandingDto CreateDefaultBrandingSettings()
    {
        return new BrandingDto
        {
            Logo = new BrandingLogoDto
            {
                Url = null,
                FileName = null,
                UploadedAt = null,
                Dimensions = new LogoDimensionsDto { Width = 0, Height = 0 }
            },
            Colors = new BrandingColorsDto
            {
                Primary = "#3B82F6", // Default blue
                Secondary = "#6B7280", // Default gray
                Accent = "#10B981", // Default green
                Text = "#1F2937", // Default dark gray
                Background = "#FFFFFF" // Default white
            },
            Typography = new BrandingTypographyDto
            {
                HeadingFont = "Inter",
                BodyFont = "Inter",
                FontSizeScale = "medium"
            },
            Style = new BrandingStyleDto
            {
                Theme = "professional",
                CustomCss = null
            },
            LastUpdated = DateTime.UtcNow
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