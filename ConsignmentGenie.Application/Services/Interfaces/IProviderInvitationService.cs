using ConsignmentGenie.Application.DTOs.Provider;
using ConsignmentGenie.Core.DTOs.Registration;

namespace ConsignmentGenie.Application.Services.Interfaces;

public interface IProviderInvitationService
{
    Task<ProviderInvitationResultDto> CreateInvitationAsync(CreateProviderInvitationDto request, Guid organizationId, Guid invitedById);
    Task<IEnumerable<ProviderInvitationDto>> GetPendingInvitationsAsync(Guid organizationId);
    Task<ProviderInvitationDto?> GetInvitationByTokenAsync(string token);
    Task<bool> CancelInvitationAsync(Guid invitationId, Guid organizationId);
    Task<bool> ResendInvitationAsync(Guid invitationId, Guid organizationId);
    Task<RegisterProviderFromInvitationResponse> RegisterFromInvitationAsync(RegisterProviderFromInvitationRequest request);
}