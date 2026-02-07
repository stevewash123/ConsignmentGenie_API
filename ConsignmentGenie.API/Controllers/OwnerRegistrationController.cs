using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OwnerRegistrationController : ControllerBase
{
    private readonly IOwnerInvitationService _ownerInvitationService;
    private readonly ILogger<OwnerRegistrationController> _logger;

    public OwnerRegistrationController(
        IOwnerInvitationService ownerInvitationService,
        ILogger<OwnerRegistrationController> logger)
    {
        _ownerInvitationService = ownerInvitationService;
        _logger = logger;
    }

    /// <summary>
    /// Validate invitation token and get invitation details
    /// </summary>
    [HttpGet("validate")]
    public async Task<ActionResult<ApiResponse<ValidateInvitationResponse>>> ValidateInvitation(
        [FromQuery] string token)
    {
        try
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest(ApiResponse<ValidateInvitationResponse>.ErrorResult("Token is required"));
            }

            var result = await _ownerInvitationService.ValidateTokenAsync(token);

            if (!result.IsValid)
            {
                return BadRequest(ApiResponse<ValidateInvitationResponse>.ErrorResult(result.ErrorMessage ?? "Invalid token"));
            }

            return Ok(ApiResponse<ValidateInvitationResponse>.SuccessResult(
                result,
                "Token validated successfully"
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating invitation token {Token}", token);
            return StatusCode(500, ApiResponse<ValidateInvitationResponse>.ErrorResult("An error occurred while validating the invitation"));
        }
    }

    /// <summary>
    /// Complete owner registration with invitation token
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<OwnerRegistrationResponse>>> RegisterOwner(
        [FromBody] OwnerRegistrationRequest request)
    {
        try
        {
            _logger.LogError("FLOW-1: Controller received registration request for Email={Email}, ShopName={ShopName}, Subdomain={Subdomain}",
                request.Email, request.ShopName, request.Subdomain);

            var result = await _ownerInvitationService.ProcessRegistrationAsync(request);

            _logger.LogError("FLOW-2: Service returned Success={Success}, Message={Message}",
                result.Success, result.Message);

            if (!result.Success)
            {
                return BadRequest(ApiResponse<OwnerRegistrationResponse>.ErrorResult(result.Message));
            }

            return Ok(ApiResponse<OwnerRegistrationResponse>.SuccessResult(
                result.Data,
                "Owner registration completed successfully"
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing owner registration for token {Token}", request.Token);
            return StatusCode(500, ApiResponse<OwnerRegistrationResponse>.ErrorResult("An error occurred while processing the registration"));
        }
    }
}