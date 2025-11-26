using ConsignmentGenie.Application.DTOs.QuickBooks;
using ConsignmentGenie.Application.Services;
using ConsignmentGenie.Core.Interfaces;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.API.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class QuickBooksController : ControllerBase
{
    private readonly IQuickBooksService _quickBooksService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<QuickBooksController> _logger;

    public QuickBooksController(
        IQuickBooksService quickBooksService,
        IUnitOfWork unitOfWork,
        ILogger<QuickBooksController> logger)
    {
        _quickBooksService = quickBooksService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    [HttpGet("connect")]
    public IActionResult GetConnectionUrl()
    {
        try
        {
            var organizationId = User.FindFirst("OrganizationId")?.Value;
            if (string.IsNullOrEmpty(organizationId))
                return BadRequest("Organization not found");

            var authUrl = _quickBooksService.GetAuthorizationUrl(organizationId);

            return Ok(new { authorizationUrl = authUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating QuickBooks connection URL");
            return StatusCode(500, "Failed to generate connection URL");
        }
    }

    [HttpPost("callback")]
    [AllowAnonymous] // Called by QuickBooks, not authenticated user
    public async Task<IActionResult> HandleCallback([FromBody] QuickBooksConnectionRequest request)
    {
        try
        {
            var tokenResponse = await _quickBooksService.ExchangeCodeForTokensAsync(
                request.Code, request.RealmId, request.State);

            // Get organization to return company info
            var organization = await _unitOfWork.Organizations.GetByIdAsync(Guid.Parse(request.State));

            return Ok(new QuickBooksConnectionResponse
            {
                Success = true,
                Message = "QuickBooks connected successfully",
                CompanyName = organization?.Name
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling QuickBooks callback");
            return BadRequest(new QuickBooksConnectionResponse
            {
                Success = false,
                Message = "Failed to connect QuickBooks"
            });
        }
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetConnectionStatus()
    {
        try
        {
            var organizationId = User.FindFirst("OrganizationId")?.Value;
            if (string.IsNullOrEmpty(organizationId))
                return BadRequest("Organization not found");

            var organization = await _unitOfWork.Organizations.GetByIdAsync(Guid.Parse(organizationId));
            if (organization == null)
                return NotFound("Organization not found");

            // Count pending sync items
            var pendingTransactions = await _unitOfWork.Transactions
                .CountAsync(t => t.OrganizationId == organization.Id && !t.SyncedToQuickBooks);

            var pendingPayouts = await _unitOfWork.Payouts
                .CountAsync(p => p.OrganizationId == organization.Id && !p.SyncedToQuickBooks);

            return Ok(new QuickBooksSyncStatusResponse
            {
                IsConnected = organization.QuickBooksConnected,
                CompanyName = organization.Name,
                LastSyncDate = organization.QuickBooksLastSync,
                PendingTransactions = pendingTransactions,
                PendingPayouts = pendingPayouts
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting QuickBooks status");
            return StatusCode(500, "Failed to get connection status");
        }
    }

    [HttpPost("sync")]
    [RequiresTier(SubscriptionTier.Pro)] // Pro feature
    public async Task<IActionResult> SyncTransactions()
    {
        try
        {
            var organizationId = User.FindFirst("OrganizationId")?.Value;
            if (string.IsNullOrEmpty(organizationId))
                return BadRequest("Organization not found");

            await _quickBooksService.SyncTransactionsAsync(organizationId);

            // Update last sync time
            var organization = await _unitOfWork.Organizations.GetByIdAsync(Guid.Parse(organizationId));
            if (organization != null)
            {
                organization.QuickBooksLastSync = DateTime.UtcNow;
                await _unitOfWork.Organizations.UpdateAsync(organization);
                await _unitOfWork.SaveChangesAsync();
            }

            return Ok(new { message = "Transactions synced successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing transactions to QuickBooks");
            return StatusCode(500, "Failed to sync transactions");
        }
    }

    [HttpPost("disconnect")]
    public async Task<IActionResult> Disconnect()
    {
        try
        {
            var organizationId = User.FindFirst("OrganizationId")?.Value;
            if (string.IsNullOrEmpty(organizationId))
                return BadRequest("Organization not found");

            var organization = await _unitOfWork.Organizations.GetByIdAsync(Guid.Parse(organizationId));
            if (organization == null)
                return NotFound("Organization not found");

            // Clear QuickBooks connection
            organization.QuickBooksConnected = false;
            organization.QuickBooksRealmId = null;
            organization.QuickBooksAccessToken = null;
            organization.QuickBooksRefreshToken = null;
            organization.QuickBooksTokenExpiry = null;
            organization.QuickBooksLastSync = null;

            await _unitOfWork.Organizations.UpdateAsync(organization);
            await _unitOfWork.SaveChangesAsync();

            return Ok(new { message = "QuickBooks disconnected successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting QuickBooks");
            return StatusCode(500, "Failed to disconnect QuickBooks");
        }
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken()
    {
        try
        {
            var organizationId = User.FindFirst("OrganizationId")?.Value;
            if (string.IsNullOrEmpty(organizationId))
                return BadRequest("Organization not found");

            var success = await _quickBooksService.RefreshTokenAsync(organizationId);

            if (!success)
                return BadRequest("Failed to refresh token. Please reconnect QuickBooks.");

            return Ok(new { message = "Token refreshed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing QuickBooks token");
            return StatusCode(500, "Failed to refresh token");
        }
    }
}