using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ConsignmentGenie.Core.DTOs.Registration;
using ConsignmentGenie.Core.Interfaces;
using System.Security.Claims;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/auth")]
public class RegistrationController : ControllerBase
{
    private readonly IRegistrationService _registrationService;
    private readonly ILogger<RegistrationController> _logger;

    public RegistrationController(IRegistrationService registrationService, ILogger<RegistrationController> logger)
    {
        _registrationService = registrationService;
        _logger = logger;
    }

    [HttpGet("validate-store-code/{code}")]
    [AllowAnonymous]
    public async Task<ActionResult<StoreCodeValidationDto>> ValidateStoreCode(string code)
    {
        var result = await _registrationService.ValidateStoreCodeAsync(code);
        return Ok(result);
    }

    [HttpPost("register/owner")]
    [AllowAnonymous]
    public async Task<ActionResult<RegistrationResultDto>> RegisterOwner([FromBody] RegisterOwnerRequest request)
    {
        _logger.LogInformation("Owner registration attempt for Email={Email}, ShopName={ShopName}",
            request.Email, request.ShopName);

        var result = await _registrationService.RegisterOwnerAsync(request);

        _logger.LogInformation("Owner registration completed. Success={Success}, Message={Message}",
            result.Success, result.Message);

        if (!result.Success)
        {
            // Return 400 Bad Request for validation errors (email exists, subdomain taken, etc.)
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("register/provider")]
    [AllowAnonymous]
    public async Task<ActionResult<RegistrationResultDto>> RegisterProvider([FromBody] RegisterProviderRequest request)
    {
        var result = await _registrationService.RegisterProviderAsync(request);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpGet("validate-invitation/{token}")]
    [AllowAnonymous]
    public async Task<ActionResult<InvitationValidationDto>> ValidateInvitation(string token)
    {
        var result = await _registrationService.ValidateInvitationTokenAsync(token);
        return Ok(result);
    }

    [HttpPost("register/provider/invitation")]
    [AllowAnonymous]
    public async Task<ActionResult<RegistrationResultDto>> RegisterProviderFromInvitation([FromBody] RegisterProviderFromInvitationRequest request)
    {
        var result = await _registrationService.RegisterProviderFromInvitationAsync(request);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}