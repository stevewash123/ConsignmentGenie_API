using ConsignmentGenie.Application.Services;
using ConsignmentGenie.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/owner/integrations/square")]
[Authorize]
public class SquareIntegrationsController : ControllerBase
{
    private readonly ISquareOAuthService _squareOAuthService;
    private readonly ISquareSyncService _squareSyncService;
    private readonly PendingImportAssignmentService _assignmentService;
    private readonly ILogger<SquareIntegrationsController> _logger;

    public SquareIntegrationsController(
        ISquareOAuthService squareOAuthService,
        ISquareSyncService squareSyncService,
        PendingImportAssignmentService assignmentService,
        ILogger<SquareIntegrationsController> logger)
    {
        _squareOAuthService = squareOAuthService;
        _squareSyncService = squareSyncService;
        _assignmentService = assignmentService;
        _logger = logger;
    }

    /// <summary>
    /// Get Square OAuth authorization URL
    /// </summary>
    [HttpGet("auth-url")]
    public async Task<IActionResult> GetAuthUrl()
    {
        try
        {
            var organizationId = GetOrganizationId();
            var authUrl = await _squareOAuthService.GenerateAuthUrlAsync(organizationId);

            return Ok(new { authUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Square auth URL");
            return StatusCode(500, new { error = "Failed to generate authorization URL" });
        }
    }

    /// <summary>
    /// Handle Square OAuth callback
    /// </summary>
    [HttpGet("callback")]
    [HttpPost("callback")]
    [AllowAnonymous] // OAuth callback comes from Square, not authenticated user
    public async Task<IActionResult> HandleCallback([FromQuery] string? code = null, [FromQuery] string? state = null, [FromQuery] string? error = null)
    {
        try
        {
            // For POST requests, try to get parameters from the request body
            if (HttpContext.Request.Method == "POST" && string.IsNullOrEmpty(code))
            {
                using var reader = new StreamReader(HttpContext.Request.Body);
                var body = await reader.ReadToEndAsync();

                try
                {
                    var json = System.Text.Json.JsonDocument.Parse(body);
                    code = json.RootElement.TryGetProperty("code", out var codeElement) ? codeElement.GetString() : null;
                    state = json.RootElement.TryGetProperty("state", out var stateElement) ? stateElement.GetString() : null;
                    error = json.RootElement.TryGetProperty("error", out var errorElement) ? errorElement.GetString() : null;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Failed to parse JSON body: {Error}", ex.Message);
                }
            }

            if (!string.IsNullOrEmpty(error))
            {
                _logger.LogWarning("Square OAuth error: {Error}", error);
                return BadRequest(new { success = false, error = error });
            }

            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            {
                _logger.LogWarning("Missing code or state in Square OAuth callback");
                return BadRequest(new { success = false, error = "invalid_request" });
            }

            // Extract organization ID from state token
            var organizationId = ExtractOrganizationIdFromState(state);
            if (organizationId == Guid.Empty)
            {
                _logger.LogWarning("Invalid state token in Square OAuth callback");
                return BadRequest(new { success = false, error = "invalid_state" });
            }

            var result = await _squareOAuthService.ProcessCallbackAsync(organizationId, code, state);

            if (result.Success)
            {
                _logger.LogInformation("Square OAuth successful for organization {OrganizationId}", organizationId);

                // Trigger initial sync after successful connection
                try
                {
                    _logger.LogInformation("Triggering initial Square sync after OAuth completion for organization {OrganizationId}", organizationId);
                    var syncResult = await _squareSyncService.SyncAllAsync(organizationId);
                    _logger.LogInformation("Initial Square sync completed for organization {OrganizationId}. Items: {ItemsSynced}, Categories: {CategoriesSynced}",
                        organizationId, syncResult.ItemsSynced, syncResult.CategoriesSynced);
                }
                catch (Exception syncEx)
                {
                    _logger.LogError(syncEx, "Error during initial Square sync after OAuth for organization {OrganizationId}", organizationId);
                    // Don't fail the OAuth callback if sync fails - connection is still established
                }

                return Ok(new { success = true, message = "Square connected successfully", merchantId = result.MerchantId });
            }
            else
            {
                _logger.LogWarning("Square OAuth failed for organization {OrganizationId}: {ErrorCode}",
                    organizationId, result.ErrorCode);
                return BadRequest(new { success = false, error = result.ErrorCode });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Square OAuth callback");
            return StatusCode(500, new { success = false, error = "internal_error" });
        }
    }

    /// <summary>
    /// Get Square connection status
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        try
        {
            var organizationId = GetOrganizationId();
            var status = await _squareOAuthService.GetConnectionStatusAsync(organizationId);

            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Square connection status");
            return StatusCode(500, new { error = "Failed to get connection status" });
        }
    }

    /// <summary>
    /// Disconnect Square integration
    /// </summary>
    [HttpPost("disconnect")]
    public async Task<IActionResult> Disconnect()
    {
        try
        {
            var organizationId = GetOrganizationId();
            var success = await _squareOAuthService.DisconnectAsync(organizationId);

            if (success)
            {
                _logger.LogInformation("Square disconnected for organization {OrganizationId}", organizationId);
                return Ok(new { message = "Square integration disconnected successfully" });
            }
            else
            {
                return BadRequest(new { error = "Failed to disconnect Square integration" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting Square integration");
            return StatusCode(500, new { error = "Failed to disconnect Square integration" });
        }
    }

    /// <summary>
    /// Manually refresh Square access token (admin use)
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken()
    {
        try
        {
            var organizationId = GetOrganizationId();
            var success = await _squareOAuthService.RefreshTokenAsync(organizationId);

            if (success)
            {
                _logger.LogInformation("Square token refreshed for organization {OrganizationId}", organizationId);
                return Ok(new { message = "Token refreshed successfully" });
            }
            else
            {
                return BadRequest(new { error = "Failed to refresh token" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing Square token");
            return StatusCode(500, new { error = "Failed to refresh token" });
        }
    }

    /// <summary>
    /// Get Square catalog items (read-only view)
    /// </summary>
    [HttpGet("catalog")]
    public async Task<IActionResult> GetCatalog([FromQuery] int page = 1, [FromQuery] int pageSize = 25, [FromQuery] string? search = null)
    {
        try
        {
            // Note: For now, just return a message that this is coming soon
            // In the full implementation, this would call a Square Catalog API read-only service
            return Ok(new { message = "Square catalog browsing coming soon", page, pageSize, search });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Square catalog");
            return StatusCode(500, new { error = "Failed to get Square catalog" });
        }
    }

    /// <summary>
    /// Get Square inventory items for ConsignmentGenie display
    /// Returns pending Square imports that need consignor assignment in PendingSquareImportDto format
    /// </summary>
    [HttpGet("inventory/items")]
    public async Task<IActionResult> GetInventoryItems([FromQuery] int page = 1, [FromQuery] int pageSize = 25, [FromQuery] string? search = null)
    {
        try
        {
            var organizationId = GetOrganizationId();

            // Get pending imports that are waiting for assignment
            var pendingImports = await _assignmentService.GetPendingImportsAsync(organizationId);

            // Apply search filter if provided
            if (!string.IsNullOrEmpty(search))
            {
                pendingImports = pendingImports.Where(p =>
                    (p.Name?.Contains(search, StringComparison.OrdinalIgnoreCase) == true) ||
                    (p.Description?.Contains(search, StringComparison.OrdinalIgnoreCase) == true) ||
                    (p.SquareCatalogId?.Contains(search, StringComparison.OrdinalIgnoreCase) == true) ||
                    (p.Sku?.Contains(search, StringComparison.OrdinalIgnoreCase) == true));
            }

            // Apply pagination
            var totalCount = pendingImports.Count();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            var paginatedImports = pendingImports
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Map to PendingSquareImportDto format - this matches the interface in UI
            var items = paginatedImports.Select(p => new ConsignmentGenie.Application.DTOs.PendingSquareImportDto
            {
                PendingImportId = p.Id,
                SquareCatalogId = p.SquareCatalogId,
                SquareVariationId = p.SquareVariationId,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                Sku = p.Sku,
                Category = p.Category,
                SquareUpdatedAt = p.SquareUpdatedAt,
                ImportedAt = p.ImportedAt,
                Status = "pending_assignment"
            }).ToList();

            // Return PagedResult<T> format expected by frontend
            var response = new
            {
                items = items,
                totalCount = totalCount,
                page = page,
                pageSize = pageSize,
                totalPages = totalPages,
                hasNextPage = page < totalPages,
                hasPreviousPage = page > 1,
                organizationId = organizationId.ToString()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Square inventory items");
            return StatusCode(500, new { error = "Failed to get Square inventory items" });
        }
    }

    /// <summary>
    /// Sync all Square inventory and categories to ConsignmentGenie
    /// </summary>
    [HttpPost("sync")]
    public async Task<IActionResult> SyncAll()
    {
        try
        {
            var organizationId = GetOrganizationId();
            var result = await _squareSyncService.SyncAllAsync(organizationId);

            _logger.LogInformation("Complete Square sync completed for organization {OrganizationId}. Items: {ItemsSynced}, Categories: {CategoriesSynced}",
                organizationId, result.ItemsSynced, result.CategoriesSynced);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing all Square data for organization {OrganizationId}", GetOrganizationId());
            return StatusCode(500, new { error = "Failed to sync all Square data" });
        }
    }

    /// <summary>
    /// Sync a specific Square item to ConsignmentGenie
    /// </summary>
    [HttpPost("sync/{itemId}")]
    public async Task<IActionResult> SyncItem(string itemId)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var result = await _squareSyncService.SyncItemAsync(organizationId, itemId);

            if (result.Success)
            {
                _logger.LogInformation("Square item {ItemId} sync completed for organization {OrganizationId}", itemId, organizationId);
                return Ok(result);
            }
            else
            {
                _logger.LogWarning("Square item {ItemId} sync failed for organization {OrganizationId}: {Error}", itemId, organizationId, result.Error);
                return BadRequest(new { error = result.Error });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing Square item {ItemId} for organization {OrganizationId}", itemId, GetOrganizationId());
            return StatusCode(500, new { error = $"Failed to sync Square item {itemId}" });
        }
    }

    /// <summary>
    /// Sync Square categories to ConsignmentGenie
    /// </summary>
    [HttpPost("categories")]
    public async Task<IActionResult> SyncCategories()
    {
        try
        {
            var organizationId = GetOrganizationId();
            var result = await _squareSyncService.SyncCategoriesAsync(organizationId);

            _logger.LogInformation("Square categories sync completed for organization {OrganizationId}. Created: {Created}, Updated: {Updated}",
                organizationId, result.Created, result.Updated);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing Square categories for organization {OrganizationId}", GetOrganizationId());
            return StatusCode(500, new { error = "Failed to sync Square categories" });
        }
    }

    /// <summary>
    /// Sync Square transactions/payments to ConsignmentGenie
    /// </summary>
    [HttpPost("transactions")]
    public async Task<IActionResult> SyncTransactions()
    {
        try
        {
            var organizationId = GetOrganizationId();
            var result = await _squareSyncService.SyncTransactionsAsync(organizationId);

            _logger.LogInformation("Square transactions sync completed for organization {OrganizationId}. " +
                "Synced: {TransactionsSynced}, Pending: {PendingCreated}, Main: {MainCreated}",
                organizationId, result.TransactionsSynced, result.PendingTransactionsCreated, result.MainTransactionsCreated);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing Square transactions for organization {OrganizationId}", GetOrganizationId());
            return StatusCode(500, new { error = "Failed to sync Square transactions" });
        }
    }

    /// <summary>
    /// Debug endpoint to check pending table status
    /// </summary>
    [HttpGet("debug/pending-status")]
    public IActionResult GetPendingStatus()
    {
        try
        {
            var organizationId = GetOrganizationId();

            // This is a simple debug endpoint - normally you'd inject repositories properly
            // For now, let's add some logging to understand what's happening
            _logger.LogInformation("Debug: Checking pending status for organization {OrganizationId}", organizationId);

            return Ok(new {
                organizationId = organizationId,
                message = "Check logs for Square sync details. Ensure Square connection exists and has recent transactions."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending status for organization {OrganizationId}", GetOrganizationId());
            return StatusCode(500, new { error = "Failed to get pending status" });
        }
    }

    /// <summary>
    /// Debug endpoint to test Square Payments API directly
    /// </summary>
    [HttpGet("debug/payments-test")]
    public async Task<IActionResult> TestSquarePayments()
    {
        try
        {
            var organizationId = GetOrganizationId();
            _logger.LogInformation("Testing Square Payments API for organization {OrganizationId}", organizationId);

            // Get last 90 days to increase chances of finding data
            var startTime = DateTime.UtcNow.AddDays(-90).ToString("yyyy-MM-ddTHH:mm:ssZ");
            var endTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

            _logger.LogInformation("Date range: {StartTime} to {EndTime}", startTime, endTime);

            // Call transaction sync to see what happens
            var result = await _squareSyncService.SyncTransactionsAsync(organizationId);

            return Ok(new {
                success = result.Success,
                transactionsSynced = result.TransactionsSynced,
                pendingCreated = result.PendingTransactionsCreated,
                mainCreated = result.MainTransactionsCreated,
                errors = result.Errors,
                dateRange = $"{startTime} to {endTime}",
                message = "Check server logs for detailed Square API responses"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing Square payments for organization {OrganizationId}", GetOrganizationId());
            return StatusCode(500, new { error = $"Failed to test Square payments: {ex.Message}" });
        }
    }

    /// <summary>
    /// DEPRECATED: Backward compatibility for old frontend - redirects to new sync endpoint
    /// </summary>
    [HttpPost("inventory/sync")]
    public async Task<IActionResult> SyncAllLegacy()
    {
        try
        {
            var organizationId = GetOrganizationId();
            var result = await _squareSyncService.SyncAllAsync(organizationId);

            _logger.LogInformation("Square inventory sync (legacy endpoint) completed for organization {OrganizationId}", organizationId);

            // Return legacy format for compatibility
            return Ok(new { message = "Square inventory synced to ConsignmentGenie successfully", result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing Square inventory (legacy endpoint) for organization {OrganizationId}", GetOrganizationId());
            return StatusCode(500, new { error = "Failed to sync Square inventory" });
        }
    }

    #region Private Helper Methods

    private Guid GetOrganizationId()
    {
        var orgClaim = User.FindFirst("organizationId");
        if (orgClaim == null || !Guid.TryParse(orgClaim.Value, out var organizationId))
        {
            throw new UnauthorizedAccessException("Organization ID not found in token");
        }
        return organizationId;
    }

    private Guid ExtractOrganizationIdFromState(string state)
    {
        try
        {
            var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(state));
            var parts = decoded.Split(':');
            if (parts.Length >= 1 && Guid.TryParse(parts[0], out var orgId))
            {
                return orgId;
            }
        }
        catch
        {
            // Invalid state token format
        }
        return Guid.Empty;
    }

    private string GetCallbackRedirectUrl(string queryParams)
    {
        // In production, this should come from configuration
        var baseUrl = "http://localhost:4200"; // Use configured client URL
        var redirectPath = "/owner/settings/integrations/inventory";
        return $"{baseUrl}{redirectPath}?{queryParams}";
    }

    #endregion
}