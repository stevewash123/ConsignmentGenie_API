using ConsignmentGenie.Application.DTOs.Payout;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Services;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using System.Reflection;

namespace ConsignmentGenie.API.Controllers.Settings;

[ApiController]
[Route("api/owner/settings/payouts")]
[Authorize(Roles = "Owner")]
public class OrganizationPayoutSettingsController : ControllerBase
{
    private readonly ConsignmentGenieContext _context;
    private readonly ILogger<OrganizationPayoutSettingsController> _logger;
    private readonly IPlaidService _plaidService;
    private readonly IDwollaService _dwollaService;

    public OrganizationPayoutSettingsController(
        ConsignmentGenieContext context,
        ILogger<OrganizationPayoutSettingsController> logger,
        IPlaidService plaidService,
        IDwollaService dwollaService)
    {
        _context = context;
        _logger = logger;
        _plaidService = plaidService;
        _dwollaService = dwollaService;
    }

    [HttpGet("general")]
    public async Task<IActionResult> GetPayoutSettings()
    {
        var organizationId = GetOrganizationId();

        var settings = await _context.PayoutSettings
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId);

        if (settings == null)
        {
            // Return default settings if none exist
            var defaultSettings = new PayoutSettingsDto
            {
                Id = Guid.Empty,
                OrganizationId = organizationId,
                PayoutMethodCheck = true,
                PayoutMethodCash = true,
                PayoutMethodACH = false,
                PayoutMethodVenmo = false,
                PayoutMethodPayPal = false,
                PayoutMethodStoreCredit = false,
                PayoutMethodZelle = false,
                HoldPeriodDays = 7,
                MinimumPayoutThreshold = 25.00m,
                MinimumBalanceProtection = 100.00m,
                BankAccountConnected = false,
                PlaidAccessToken = string.Empty,
                PlaidAccountId = string.Empty,
                BankName = string.Empty,
                BankAccountLast4 = string.Empty,
                AutoPayEnabled = false,
                AutoPayMonday = false,
                AutoPayTuesday = false,
                AutoPayWednesday = false,
                AutoPayThursday = false,
                AutoPayFriday = false,
                AutoPaySaturday = false,
                AutoPaySunday = false,
                ExtendedSettings = "{}",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            return Ok(defaultSettings);
        }

        var settingsDto = MapToDto(settings);
        return Ok(settingsDto);
    }

    [HttpPost("general")]
    public async Task<IActionResult> CreatePayoutSettings([FromBody] CreatePayoutSettingsRequest request)
    {
        var organizationId = GetOrganizationId();

        // Check if settings already exist
        var existingSettings = await _context.PayoutSettings
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId);

        if (existingSettings != null)
        {
            return BadRequest(new { success = false, message = "New payout settings already exist for this organization. Use PUT to update." });
        }

        var settings = new PayoutSettings
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            PayoutMethodCheck = request.PayoutMethodCheck,
            PayoutMethodCash = request.PayoutMethodCash,
            PayoutMethodACH = request.PayoutMethodACH,
            PayoutMethodVenmo = request.PayoutMethodVenmo,
            PayoutMethodPayPal = request.PayoutMethodPayPal,
            PayoutMethodStoreCredit = request.PayoutMethodStoreCredit,
            PayoutMethodZelle = request.PayoutMethodZelle,
            HoldPeriodDays = request.HoldPeriodDays,
            MinimumPayoutThreshold = request.MinimumPayoutThreshold,
            MinimumBalanceProtection = request.MinimumBalanceProtection,
            BankAccountConnected = request.BankAccountConnected,
            PlaidAccessToken = request.PlaidAccessToken,
            PlaidAccountId = request.PlaidAccountId,
            BankName = request.BankName,
            BankAccountLast4 = request.BankAccountLast4,
            AutoPayEnabled = request.AutoPayEnabled,
            AutoPayMonday = request.AutoPayMonday,
            AutoPayTuesday = request.AutoPayTuesday,
            AutoPayWednesday = request.AutoPayWednesday,
            AutoPayThursday = request.AutoPayThursday,
            AutoPayFriday = request.AutoPayFriday,
            AutoPaySaturday = request.AutoPaySaturday,
            AutoPaySunday = request.AutoPaySunday,
            ExtendedSettings = request.ExtendedSettings,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.PayoutSettings.Add(settings);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created new payout settings for organization {OrganizationId}", organizationId);

        var settingsDto = MapToDto(settings);
        return CreatedAtAction(nameof(GetPayoutSettings), new { id = settings.Id }, settingsDto);
    }

    [HttpPut("general")]
    public async Task<IActionResult> UpdatePayoutSettings([FromBody] UpdatePayoutSettingsRequest request)
    {
        var organizationId = GetOrganizationId();

        var settings = await _context.PayoutSettings
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId);

        if (settings == null)
        {
            // Create new settings with default values if none exist, then apply updates
            settings = new PayoutSettings
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                PayoutMethodCheck = true,
                PayoutMethodCash = true,
                PayoutMethodACH = false,
                PayoutMethodVenmo = false,
                PayoutMethodPayPal = false,
                PayoutMethodStoreCredit = false,
                PayoutMethodZelle = false,
                HoldPeriodDays = 7,
                MinimumPayoutThreshold = 25.00m,
                MinimumBalanceProtection = 100.00m,
                BankAccountConnected = false,
                PlaidAccessToken = string.Empty,
                PlaidAccountId = string.Empty,
                BankName = string.Empty,
                BankAccountLast4 = string.Empty,
                AutoPayEnabled = false,
                AutoPayMonday = false,
                AutoPayTuesday = false,
                AutoPayWednesday = false,
                AutoPayThursday = false,
                AutoPayFriday = false,
                AutoPaySaturday = false,
                AutoPaySunday = false,
                ExtendedSettings = "{}",
                CreatedAt = DateTime.UtcNow
            };
            _context.PayoutSettings.Add(settings);
        }

        // Apply updates using reflection to handle any property
        UpdateEntityFromRequest(settings, request);
        settings.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated new payout settings for organization {OrganizationId}", organizationId);

        var settingsDto = MapToDto(settings);
        return Ok(settingsDto);
    }

    // Special endpoint for partial updates (auto-save functionality)
    [HttpPatch("general")]
    public async Task<IActionResult> PatchPayoutSettings([FromBody] JsonElement updates)
    {
        var organizationId = GetOrganizationId();

        var settings = await _context.PayoutSettings
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId);

        if (settings == null)
        {
            // Create with defaults first
            settings = new PayoutSettings
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                PayoutMethodCheck = true,
                PayoutMethodCash = true,
                PayoutMethodACH = false,
                PayoutMethodVenmo = false,
                PayoutMethodPayPal = false,
                PayoutMethodStoreCredit = false,
                PayoutMethodZelle = false,
                HoldPeriodDays = 7,
                MinimumPayoutThreshold = 25.00m,
                MinimumBalanceProtection = 100.00m,
                BankAccountConnected = false,
                PlaidAccessToken = string.Empty,
                PlaidAccountId = string.Empty,
                BankName = string.Empty,
                BankAccountLast4 = string.Empty,
                AutoPayEnabled = false,
                AutoPayMonday = false,
                AutoPayTuesday = false,
                AutoPayWednesday = false,
                AutoPayThursday = false,
                AutoPayFriday = false,
                AutoPaySaturday = false,
                AutoPaySunday = false,
                ExtendedSettings = "{}",
                CreatedAt = DateTime.UtcNow
            };
            _context.PayoutSettings.Add(settings);
        }

        // Apply JSON patch-like updates
        ApplyJsonUpdates(settings, updates);
        settings.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Patched new payout settings for organization {OrganizationId}", organizationId);

        var settingsDto = MapToDto(settings);
        return Ok(settingsDto);
    }

    [HttpDelete("general")]
    public async Task<IActionResult> DeletePayoutSettings()
    {
        var organizationId = GetOrganizationId();

        var settings = await _context.PayoutSettings
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId);

        if (settings == null)
        {
            return NotFound(new { success = false, message = "New payout settings not found" });
        }

        _context.PayoutSettings.Remove(settings);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted new payout settings for organization {OrganizationId}", organizationId);

        return Ok(new { success = true, message = "New payout settings deleted successfully" });
    }

    /// <summary>
    /// Create a Plaid Link token for bank connection
    /// </summary>
    [HttpPost("direct-deposit/plaid/link-token")]
    public async Task<ActionResult<PlaidLinkTokenResponse>> CreatePlaidLinkToken()
    {
        var organizationId = GetOrganizationId();
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return BadRequest("Invalid user ID");
        }

        // Verify ACH is enabled in payout settings
        var payoutSettings = await _context.PayoutSettings
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId);

        if (payoutSettings == null || !payoutSettings.PayoutMethodACH)
        {
            return BadRequest(new { success = false, message = "ACH must be enabled first" });
        }

        var linkToken = await _plaidService.CreateLinkTokenAsync(organizationId, userId);

        return Ok(linkToken);
    }

    /// <summary>
    /// Exchange Plaid public token and connect bank account
    /// </summary>
    [HttpPost("direct-deposit/plaid/exchange-token")]
    public async Task<ActionResult<BankAccountLinkedResponse>> ExchangePlaidToken(
        [FromBody] PlaidExchangeTokenRequest request)
    {
        var organizationId = GetOrganizationId();

        // Check if ACH is enabled
        var payoutSettings = await _context.PayoutSettings
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId);

        if (payoutSettings == null || !payoutSettings.PayoutMethodACH)
        {
            return BadRequest(new { success = false, message = "ACH must be enabled first" });
        }

        // Ensure Dwolla customer exists
        if (string.IsNullOrEmpty(payoutSettings.DwollaCustomerUrl))
        {
            // Create Dwolla customer if needed
            var org = await _context.Organizations.FindAsync(organizationId);
            var userEmailClaim = User.FindFirst(ClaimTypes.Email)?.Value;

            if (org != null && userEmailClaim != null)
            {
                var dwollaResult = await _dwollaService.CreateOrGetCustomerAsync(
                    organizationId,
                    org.Name,
                    userEmailClaim);

                if (dwollaResult.Success)
                {
                    payoutSettings.DwollaCustomerId = dwollaResult.CustomerId ?? string.Empty;
                    payoutSettings.DwollaCustomerUrl = dwollaResult.CustomerUrl ?? string.Empty;
                    payoutSettings.DwollaCustomerStatus = dwollaResult.Status ?? string.Empty;
                    await _context.SaveChangesAsync();
                }
                else
                {
                    return BadRequest(new { success = false, message = "Failed to set up Dwolla customer" });
                }
            }
            else
            {
                return BadRequest(new { success = false, message = "Organization or email not found" });
            }
        }

        // Exchange Plaid token
        var plaidResult = await _plaidService.ExchangePublicTokenAsync(
            request.PublicToken, request.AccountId);

        if (!plaidResult.Success)
        {
            return BadRequest(new { success = false, message = plaidResult.ErrorMessage });
        }

        // Create Dwolla funding source
        var accountName = !string.IsNullOrEmpty(request.AccountName)
            ? request.AccountName
            : $"{request.InstitutionName} {request.AccountType}";

        var dwollaFundingResult = await _dwollaService.CreateFundingSourceAsync(
            payoutSettings.DwollaCustomerUrl,
            plaidResult.ProcessorToken!,
            accountName);

        if (!dwollaFundingResult.Success)
        {
            return BadRequest(new { success = false, message = dwollaFundingResult.ErrorMessage });
        }

        // Update payout settings with bank connection info
        payoutSettings.BankAccountConnected = true;
        payoutSettings.PlaidAccessToken = plaidResult.AccessToken ?? string.Empty;
        payoutSettings.PlaidAccountId = request.AccountId;
        payoutSettings.BankName = request.InstitutionName ?? "Connected Bank";
        payoutSettings.BankAccountLast4 = request.AccountMask ?? "";
        payoutSettings.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new BankAccountLinkedResponse
        {
            Success = true,
            Message = "Bank account connected successfully",
            FundingSource = new FundingSourceDto
            {
                Id = dwollaFundingResult.FundingSourceId!,
                BankName = request.InstitutionName ?? "Connected Bank",
                AccountType = request.AccountType ?? "checking",
                AccountNumberMask = request.AccountMask ?? "••••",
                Status = "connected",
                IsDefault = true,
                ConnectedAt = DateTime.UtcNow
            }
        });
    }

    /// <summary>
    /// Disconnect bank account
    /// </summary>
    [HttpPost("direct-deposit/bank/disconnect")]
    public async Task<ActionResult> DisconnectBank()
    {
        var organizationId = GetOrganizationId();

        var payoutSettings = await _context.PayoutSettings
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId);

        if (payoutSettings == null)
        {
            return BadRequest("Payout settings not found");
        }

        // Update settings to mark bank as disconnected
        payoutSettings.BankAccountConnected = false;
        payoutSettings.PlaidAccessToken = string.Empty;
        payoutSettings.PlaidAccountId = string.Empty;
        payoutSettings.BankName = string.Empty;
        payoutSettings.BankAccountLast4 = string.Empty;
        payoutSettings.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Bank account disconnected" });
    }

    private Guid GetOrganizationId()
    {
        var organizationIdClaim = User.FindFirst("organizationId")?.Value;
        if (!Guid.TryParse(organizationIdClaim, out var organizationId))
        {
            throw new UnauthorizedAccessException("Invalid organization");
        }
        return organizationId;
    }

    private PayoutSettingsDto MapToDto(PayoutSettings settings)
    {
        return new PayoutSettingsDto
        {
            Id = settings.Id,
            OrganizationId = settings.OrganizationId,
            PayoutMethodCheck = settings.PayoutMethodCheck,
            PayoutMethodCash = settings.PayoutMethodCash,
            PayoutMethodACH = settings.PayoutMethodACH,
            PayoutMethodVenmo = settings.PayoutMethodVenmo,
            PayoutMethodPayPal = settings.PayoutMethodPayPal,
            PayoutMethodStoreCredit = settings.PayoutMethodStoreCredit,
            PayoutMethodZelle = settings.PayoutMethodZelle,
            HoldPeriodDays = settings.HoldPeriodDays,
            MinimumPayoutThreshold = settings.MinimumPayoutThreshold,
            MinimumBalanceProtection = settings.MinimumBalanceProtection,
            BankAccountConnected = settings.BankAccountConnected,
            PlaidAccessToken = settings.PlaidAccessToken,
            PlaidAccountId = settings.PlaidAccountId,
            BankName = settings.BankName,
            BankAccountLast4 = settings.BankAccountLast4,
            AutoPayEnabled = settings.AutoPayEnabled,
            AutoPayMonday = settings.AutoPayMonday,
            AutoPayTuesday = settings.AutoPayTuesday,
            AutoPayWednesday = settings.AutoPayWednesday,
            AutoPayThursday = settings.AutoPayThursday,
            AutoPayFriday = settings.AutoPayFriday,
            AutoPaySaturday = settings.AutoPaySaturday,
            AutoPaySunday = settings.AutoPaySunday,
            ExtendedSettings = settings.ExtendedSettings,
            CreatedAt = settings.CreatedAt,
            UpdatedAt = settings.UpdatedAt
        };
    }

    private void UpdateEntityFromRequest(PayoutSettings entity, UpdatePayoutSettingsRequest request)
    {
        var entityType = typeof(PayoutSettings);
        var requestType = typeof(UpdatePayoutSettingsRequest);

        foreach (var requestProp in requestType.GetProperties())
        {
            var value = requestProp.GetValue(request);
            if (value != null)
            {
                var entityProp = entityType.GetProperty(requestProp.Name);
                if (entityProp != null && entityProp.CanWrite)
                {
                    // Handle nullable types
                    var actualValue = requestProp.PropertyType.IsGenericType &&
                                    requestProp.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>)
                                    ? value
                                    : value;
                    entityProp.SetValue(entity, actualValue);
                }
            }
        }
    }

    private void ApplyJsonUpdates(PayoutSettings entity, JsonElement updates)
    {
        var entityType = typeof(PayoutSettings);

        foreach (var property in updates.EnumerateObject())
        {
            var entityProp = entityType.GetProperty(property.Name, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (entityProp != null && entityProp.CanWrite)
            {
                try
                {
                    var value = ConvertJsonValue(property.Value, entityProp.PropertyType);
                    entityProp.SetValue(entity, value);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Failed to update property {PropertyName}: {Error}", property.Name, ex.Message);
                }
            }
        }
    }

    private object? ConvertJsonValue(JsonElement jsonValue, Type targetType)
    {
        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        return underlyingType.Name switch
        {
            nameof(Boolean) => jsonValue.GetBoolean(),
            nameof(Int32) => jsonValue.GetInt32(),
            nameof(Decimal) => jsonValue.GetDecimal(),
            nameof(String) => jsonValue.GetString(),
            nameof(DateTime) => jsonValue.GetDateTime(),
            nameof(Guid) => jsonValue.GetGuid(),
            _ => JsonSerializer.Deserialize(jsonValue.GetRawText(), targetType)
        };
    }
}

// DTOs for Plaid integration
public class PlaidLinkTokenResponse
{
    public string LinkToken { get; set; } = string.Empty;
    public DateTime Expiration { get; set; }
}

public class PlaidExchangeTokenRequest
{
    public string PublicToken { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public string? AccountName { get; set; }
    public string? InstitutionName { get; set; }
    public string? AccountType { get; set; }
    public string? AccountMask { get; set; }
}

public class BankAccountLinkedResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public FundingSourceDto FundingSource { get; set; } = new();
}

public class FundingSourceDto
{
    public string Id { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public string AccountNumberMask { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public DateTime ConnectedAt { get; set; }
}