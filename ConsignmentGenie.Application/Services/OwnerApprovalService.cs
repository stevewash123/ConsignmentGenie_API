using BCrypt.Net;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.DTOs.Settings;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ConsignmentGenie.Application.Services;

public class OwnerApprovalService : IOwnerApprovalService
{
    private readonly ConsignmentGenieContext _context;
    private readonly ILogger<OwnerApprovalService> _logger;

    public OwnerApprovalService(
        ConsignmentGenieContext context,
        ILogger<OwnerApprovalService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> RequiresApprovalAsync(Guid organizationId, ApprovalAction action, decimal? amount = null)
    {
        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
                return false;

            return action switch
            {
                ApprovalAction.TaxExempt => organization.RequirePinForTaxExempt,
                ApprovalAction.LargeReturn => amount.HasValue && amount.Value > organization.RequirePinForReturnsOver,
                ApprovalAction.Void => organization.RequirePinForVoid,
                ApprovalAction.DrawerOpen => organization.RequirePinForDrawerOpen,
                _ => false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking approval requirement for organization {OrganizationId}, action {Action}",
                organizationId, action);
            return false;
        }
    }

    public async Task<ApprovalResult> ValidatePinAsync(Guid organizationId, string pin, ApprovalAction action)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(pin))
            {
                return new ApprovalResult { Approved = false, Reason = "PIN is required" };
            }

            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                return new ApprovalResult { Approved = false, Reason = "Organization not found" };
            }

            if (string.IsNullOrWhiteSpace(organization.OwnerPin))
            {
                return new ApprovalResult { Approved = false, Reason = "No PIN configured" };
            }

            // Validate PIN format (4-6 digits)
            if (pin.Length < 4 || pin.Length > 6 || !pin.All(char.IsDigit))
            {
                return new ApprovalResult { Approved = false, Reason = "Invalid PIN format" };
            }

            // Verify PIN
            bool isValid = BCrypt.Net.BCrypt.Verify(pin, organization.OwnerPin);

            return new ApprovalResult { Approved = isValid, Reason = isValid ? null : "Invalid PIN" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating PIN for organization {OrganizationId}, action {Action}",
                organizationId, action);
            return new ApprovalResult { Approved = false, Reason = "Validation error" };
        }
    }

    public async Task<SecuritySettingsDto> GetSecuritySettingsAsync(Guid organizationId)
    {
        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                return new SecuritySettingsDto();
            }

            return new SecuritySettingsDto
            {
                HasPin = !string.IsNullOrWhiteSpace(organization.OwnerPin),
                PinRequirements = new PinRequirementsDto
                {
                    TaxExempt = organization.RequirePinForTaxExempt,
                    ReturnsOver = organization.RequirePinForReturnsOver,
                    Void = organization.RequirePinForVoid,
                    DrawerOpen = organization.RequirePinForDrawerOpen
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting security settings for organization {OrganizationId}", organizationId);
            return new SecuritySettingsDto();
        }
    }

    public async Task<bool> SetPinAsync(Guid organizationId, string pin, string currentPassword, Guid userId)
    {
        try
        {
            // Validate PIN format (4-6 digits)
            if (string.IsNullOrWhiteSpace(pin) || pin.Length < 4 || pin.Length > 6 || !pin.All(char.IsDigit))
            {
                return false;
            }

            // Verify current password
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.OrganizationId == organizationId);

            if (user == null || !BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            {
                return false;
            }

            // Hash and set PIN
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                return false;
            }

            organization.OwnerPin = BCrypt.Net.BCrypt.HashPassword(pin);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Owner PIN set for organization {OrganizationId} by user {UserId}",
                organizationId, userId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting PIN for organization {OrganizationId}", organizationId);
            return false;
        }
    }

    public async Task<bool> UpdatePinRequirementsAsync(Guid organizationId, UpdatePinRequirementsRequest request)
    {
        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                return false;
            }

            organization.RequirePinForTaxExempt = request.TaxExempt;
            organization.RequirePinForReturnsOver = request.ReturnsOver;
            organization.RequirePinForVoid = request.Void;
            organization.RequirePinForDrawerOpen = request.DrawerOpen;

            await _context.SaveChangesAsync();

            _logger.LogInformation("PIN requirements updated for organization {OrganizationId}", organizationId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating PIN requirements for organization {OrganizationId}", organizationId);
            return false;
        }
    }
}