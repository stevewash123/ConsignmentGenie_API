using ConsignmentGenie.Core.DTOs.Registration;

namespace ConsignmentGenie.Core.Interfaces;

public interface IRegistrationService
{
    Task<StoreCodeValidationDto> ValidateStoreCodeAsync(string code);
    Task<RegistrationResultDto> RegisterOwnerAsync(RegisterOwnerRequest request);
    Task<RegistrationResultDto> RegisterProviderAsync(RegisterConsignorRequest request);
    Task<List<PendingApprovalDto>> GetPendingProvidersAsync(Guid organizationId);
    Task<int> GetPendingApprovalCountAsync(Guid organizationId);
    Task ApproveUserAsync(Guid userId, Guid approvedByUserId);
    Task RejectUserAsync(Guid userId, Guid rejectedByUserId, string? reason);
    Task<List<PendingOwnerDto>> GetPendingOwnersAsync();
    Task ApproveOwnerAsync(Guid userId, Guid approvedByUserId);
    Task RejectOwnerAsync(Guid userId, Guid rejectedByUserId, string? reason);
    Task<InvitationValidationDto> ValidateInvitationTokenAsync(string token);
    Task<RegistrationResultDto> RegisterProviderFromInvitationAsync(RegisterConsignorFromInvitationRequest request);
}