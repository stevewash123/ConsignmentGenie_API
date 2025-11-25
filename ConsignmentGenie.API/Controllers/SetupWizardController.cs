using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Core.DTOs.SetupWizard;
using ConsignmentGenie.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Owner")]
public class SetupWizardController : ControllerBase
{
    private readonly ISetupWizardService _setupWizardService;
    private readonly ILogger<SetupWizardController> _logger;

    public SetupWizardController(ISetupWizardService setupWizardService, ILogger<SetupWizardController> logger)
    {
        _setupWizardService = setupWizardService;
        _logger = logger;
    }

    /// <summary>
    /// Get the overall wizard progress and status
    /// </summary>
    [HttpGet("progress")]
    public async Task<ActionResult<ApiResponse<SetupWizardProgressDto>>> GetWizardProgress()
    {
        try
        {
            var organizationId = GetOrganizationId();
            var progress = await _setupWizardService.GetWizardProgressAsync(organizationId);

            return Ok(ApiResponse<SetupWizardProgressDto>.SuccessResult(progress, "Wizard progress retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting wizard progress");
            return StatusCode(500, ApiResponse<SetupWizardProgressDto>.ErrorResult("Failed to get wizard progress"));
        }
    }

    /// <summary>
    /// Get details for a specific wizard step
    /// </summary>
    [HttpGet("step/{stepNumber:int}")]
    public async Task<ActionResult<ApiResponse<SetupWizardStepDto>>> GetWizardStep(int stepNumber)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var step = await _setupWizardService.GetWizardStepAsync(organizationId, stepNumber);

            return Ok(ApiResponse<SetupWizardStepDto>.SuccessResult(step, "Wizard step retrieved successfully"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<SetupWizardStepDto>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting wizard step {StepNumber}", stepNumber);
            return StatusCode(500, ApiResponse<SetupWizardStepDto>.ErrorResult("Failed to get wizard step"));
        }
    }

    /// <summary>
    /// Update shop profile (Step 1)
    /// </summary>
    [HttpPost("step/1/shop-profile")]
    public async Task<ActionResult<ApiResponse<SetupWizardStepDto>>> UpdateShopProfile([FromBody] UpdateShopProfileRequest request)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var result = await _setupWizardService.UpdateShopProfileAsync(organizationId, request.ShopProfile);

            return Ok(ApiResponse<SetupWizardStepDto>.SuccessResult(result, "Shop profile updated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating shop profile");
            return StatusCode(500, ApiResponse<SetupWizardStepDto>.ErrorResult("Failed to update shop profile"));
        }
    }

    /// <summary>
    /// Update business settings (Step 2)
    /// </summary>
    [HttpPost("step/2/business-settings")]
    public async Task<ActionResult<ApiResponse<SetupWizardStepDto>>> UpdateBusinessSettings([FromBody] UpdateBusinessSettingsRequest request)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var result = await _setupWizardService.UpdateBusinessSettingsAsync(organizationId, request.BusinessSettings);

            return Ok(ApiResponse<SetupWizardStepDto>.SuccessResult(result, "Business settings updated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating business settings");
            return StatusCode(500, ApiResponse<SetupWizardStepDto>.ErrorResult("Failed to update business settings"));
        }
    }

    /// <summary>
    /// Update storefront settings (Step 3)
    /// </summary>
    [HttpPost("step/3/storefront-settings")]
    public async Task<ActionResult<ApiResponse<SetupWizardStepDto>>> UpdateStorefrontSettings([FromBody] UpdateStorefrontSettingsRequest request)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var result = await _setupWizardService.UpdateStorefrontSettingsAsync(organizationId, request.StorefrontSettings);

            return Ok(ApiResponse<SetupWizardStepDto>.SuccessResult(result, "Storefront settings updated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating storefront settings");
            return StatusCode(500, ApiResponse<SetupWizardStepDto>.ErrorResult("Failed to update storefront settings"));
        }
    }

    /// <summary>
    /// Get integration status for steps 4-7
    /// </summary>
    [HttpGet("integrations")]
    public async Task<ActionResult<ApiResponse<IntegrationStatusDto>>> GetIntegrationStatus()
    {
        try
        {
            var organizationId = GetOrganizationId();
            var status = await _setupWizardService.GetIntegrationStatusAsync(organizationId);

            return Ok(ApiResponse<IntegrationStatusDto>.SuccessResult(status, "Integration status retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting integration status");
            return StatusCode(500, ApiResponse<IntegrationStatusDto>.ErrorResult("Failed to get integration status"));
        }
    }

    /// <summary>
    /// Setup an integration (Steps 4-7)
    /// </summary>
    [HttpPost("integrations/{integrationType}")]
    public async Task<ActionResult<ApiResponse<SetupWizardStepDto>>> SetupIntegration(string integrationType, [FromBody] SetupIntegrationRequest request)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var result = await _setupWizardService.SetupIntegrationAsync(organizationId, integrationType, request.Credentials);

            return Ok(ApiResponse<SetupWizardStepDto>.SuccessResult(result, $"{integrationType} integration setup successfully"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<SetupWizardStepDto>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting up {IntegrationType} integration", integrationType);
            return StatusCode(500, ApiResponse<SetupWizardStepDto>.ErrorResult($"Failed to setup {integrationType} integration"));
        }
    }

    /// <summary>
    /// Complete the setup wizard (Step 8)
    /// </summary>
    [HttpPost("complete")]
    public async Task<ActionResult<ApiResponse<SetupCompleteDto>>> CompleteSetup([FromBody] CompleteSetupRequest request)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var result = await _setupWizardService.CompleteSetupAsync(organizationId, request.StartTrial, request.SubscriptionPlan);

            return Ok(ApiResponse<SetupCompleteDto>.SuccessResult(result, "Setup wizard completed successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing setup");
            return StatusCode(500, ApiResponse<SetupCompleteDto>.ErrorResult("Failed to complete setup"));
        }
    }

    /// <summary>
    /// Move to a specific step in the wizard
    /// </summary>
    [HttpPost("step/{stepNumber:int}/goto")]
    public async Task<ActionResult<ApiResponse<SetupWizardStepDto>>> GoToStep(int stepNumber)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var result = await _setupWizardService.MoveToStepAsync(organizationId, stepNumber);

            return Ok(ApiResponse<SetupWizardStepDto>.SuccessResult(result, $"Moved to step {stepNumber}"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<SetupWizardStepDto>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving to step {StepNumber}", stepNumber);
            return StatusCode(500, ApiResponse<SetupWizardStepDto>.ErrorResult("Failed to move to step"));
        }
    }

    private Guid GetOrganizationId()
    {
        // Extract organization ID from JWT token
        var organizationIdClaim = User.FindFirst("organizationId")?.Value;
        if (organizationIdClaim != null && Guid.TryParse(organizationIdClaim, out var organizationId))
        {
            return organizationId;
        }

        throw new UnauthorizedAccessException("Organization ID not found in token");
    }
}