using ConsignmentGenie.Application.DTOs.Clerk;

namespace ConsignmentGenie.Application.Services.Interfaces;

public interface IClerkInvitationService
{
    Task<ClerkInvitationResultDto> CreateInvitationAsync(CreateClerkInvitationDto request, Guid organizationId, Guid invitedById);
    Task<IEnumerable<ClerkInvitationDto>> GetPendingInvitationsAsync(Guid organizationId);
    Task<ClerkInvitationDto?> GetInvitationByTokenAsync(string token);
    Task<bool> CancelInvitationAsync(Guid invitationId, Guid organizationId);
    Task<bool> ResendInvitationAsync(Guid invitationId, Guid organizationId);
}