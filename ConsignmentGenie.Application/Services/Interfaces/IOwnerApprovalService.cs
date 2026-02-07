using ConsignmentGenie.Core.DTOs.Settings;
using ConsignmentGenie.Core.Enums;

namespace ConsignmentGenie.Application.Services.Interfaces;

public interface IOwnerApprovalService
{
    Task<bool> RequiresApprovalAsync(Guid organizationId, ApprovalAction action, decimal? amount = null);
    Task<ApprovalResult> ValidatePinAsync(Guid organizationId, string pin, ApprovalAction action);
    Task<SecuritySettingsDto> GetSecuritySettingsAsync(Guid organizationId);
    Task<bool> SetPinAsync(Guid organizationId, string pin, string currentPassword, Guid userId);
    Task<bool> UpdatePinRequirementsAsync(Guid organizationId, UpdatePinRequirementsRequest request);
}