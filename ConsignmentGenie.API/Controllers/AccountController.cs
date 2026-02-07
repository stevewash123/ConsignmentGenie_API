using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Application.DTOs.Account;
using ConsignmentGenie.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "owner")]
public class AccountController : ControllerBase
{
    private readonly IAccountService _accountService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(IAccountService accountService, ILogger<AccountController> logger)
    {
        _accountService = accountService;
        _logger = logger;
    }

    private Guid GetOrganizationId()
    {
        var organizationIdClaim = User.FindFirst("organizationId")?.Value;
        return Guid.TryParse(organizationIdClaim, out var orgId) ? orgId : Guid.Empty;
    }

    /// <summary>
    /// Get comprehensive account information including organization details, subscription plan, usage statistics, and billing history
    /// </summary>
    [HttpGet("information")]
    public async Task<ActionResult<ApiResponse<AccountInfoDto>>> GetAccountInformation()
    {
        try
        {
            var organizationId = GetOrganizationId();
            if (organizationId == Guid.Empty)
            {
                return BadRequest(ApiResponse<AccountInfoDto>.ErrorResult("Organization ID not found in token"));
            }

            _logger.LogInformation("Getting account information for organization {OrganizationId}", organizationId);

            var accountInfo = await _accountService.GetAccountInfoAsync(organizationId);
            return Ok(ApiResponse<AccountInfoDto>.SuccessResult(accountInfo));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Organization not found");
            return NotFound(ApiResponse<AccountInfoDto>.ErrorResult($"Organization not found: {ex.Message}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get account information");
            return BadRequest(ApiResponse<AccountInfoDto>.ErrorResult($"Failed to get account information: {ex.Message}"));
        }
    }
}