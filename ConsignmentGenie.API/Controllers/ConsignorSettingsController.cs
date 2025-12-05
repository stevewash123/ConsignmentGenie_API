using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ConsignmentGenie.Infrastructure.Data;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Application.DTOs;
using System.Security.Claims;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/consignors")]
[Authorize(Roles = "Owner")]
public class ConsignorSettingsController : ControllerBase
{
    private readonly ConsignmentGenieContext _context;
    private readonly ILogger<ConsignorSettingsController> _logger;

    public ConsignorSettingsController(ConsignmentGenieContext context, ILogger<ConsignorSettingsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GENERATE NUMBER - Get next available provider number
    [HttpGet("generate-number")]
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
    [HttpGet("store-code")]
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
    [HttpPost("store-code/toggle")]
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

    // REGENERATE STORE CODE - Generate new store code
    [HttpPost("store-code/regenerate")]
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
    [HttpGet("settings")]
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
                DefaultCommissionRate = organization.DefaultSplitPercentage / 100m // Convert percentage to decimal
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
        var orgIdClaim = User.FindFirst("OrganizationId")?.Value;
        return orgIdClaim != null ? Guid.Parse(orgIdClaim) : Guid.Empty;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        return userIdClaim != null ? Guid.Parse(userIdClaim) : Guid.Empty;
    }

    #endregion
}