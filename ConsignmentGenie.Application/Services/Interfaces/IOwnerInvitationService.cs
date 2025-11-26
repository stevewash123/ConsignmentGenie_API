using ConsignmentGenie.Application.DTOs;

namespace ConsignmentGenie.Application.Services.Interfaces;

public interface IOwnerInvitationService
{
    Task<ServiceResult<OwnerInvitationDetailDto>> CreateInvitationAsync(CreateOwnerInvitationRequest request, Guid invitedById);
    Task<ConsignmentGenie.Core.DTOs.PagedResult<OwnerInvitationListDto>> GetInvitationsAsync(OwnerInvitationQueryParams queryParams);
    Task<OwnerInvitationDetailDto?> GetInvitationByIdAsync(Guid invitationId);
    Task<ValidateInvitationResponse> ValidateTokenAsync(string token);
    Task<ServiceResult<OwnerRegistrationResponse>> ProcessRegistrationAsync(OwnerRegistrationRequest request);
    Task<ServiceResult<bool>> CancelInvitationAsync(Guid invitationId);
    Task<ServiceResult<bool>> ResendInvitationAsync(Guid invitationId);
    Task<OwnerInvitationMetricsDto> GetMetricsAsync();
}