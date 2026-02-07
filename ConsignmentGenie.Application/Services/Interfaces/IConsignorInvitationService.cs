using ConsignmentGenie.Application.DTOs.Consignor;
using ConsignmentGenie.Core.DTOs.Registration;

namespace ConsignmentGenie.Application.Services.Interfaces;

public interface IConsignorInvitationService
{
    Task<ConsignorInvitationResultDto> CreateInvitationAsync(CreateConsignorInvitationDto request, Guid organizationId, Guid invitedById);
    Task<IEnumerable<ConsignorInvitationDto>> GetPendingInvitationsAsync(Guid organizationId);
    Task<ConsignorInvitationDto?> GetInvitationByTokenAsync(string token);
    Task<bool> CancelInvitationAsync(Guid invitationId, Guid organizationId);
    Task<bool> ResendInvitationAsync(Guid invitationId, Guid organizationId);
    Task<RegisterConsignorFromInvitationResponse> RegisterFromInvitationAsync(RegisterConsignorFromInvitationRequest request);
}