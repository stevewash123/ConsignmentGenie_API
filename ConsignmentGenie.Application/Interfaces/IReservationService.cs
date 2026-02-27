using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Application.DTOs.Reservation;
using ConsignmentGenie.Core.Enums;

namespace ConsignmentGenie.Application.Interfaces;

public interface IReservationService
{
    // Create and manage reservations
    Task<ServiceResult<ReservationDto>> CreateReservationAsync(CreateReservationRequest request, Guid organizationId, Guid? userId = null);
    Task<ServiceResult<ReservationDto>> GetReservationAsync(Guid reservationId, Guid organizationId);
    Task<ServiceResult<PagedResult<ReservationSummaryDto>>> GetReservationsAsync(Guid organizationId, int page = 1, int pageSize = 20, ReservationStatus? status = null, string? customerPhone = null);
    Task<ServiceResult<ReservationDto>> UpdateReservationStatusAsync(UpdateReservationStatusRequest request, Guid organizationId, Guid? userId = null);
    Task<ServiceResult<bool>> DeleteReservationAsync(Guid reservationId, Guid organizationId, Guid? userId = null);

    // SMS verification
    Task<ServiceResult<bool>> SendVerificationCodeAsync(Guid reservationId, Guid organizationId);
    Task<ServiceResult<ReservationDto>> VerifyPhoneAsync(VerifyPhoneRequest request, Guid organizationId);

    // Item availability and reservation management
    Task<ServiceResult<bool>> CheckItemsAvailabilityAsync(List<Guid> itemIds, Guid organizationId);
    Task<ServiceResult<List<Guid>>> GetReservedItemIdsAsync(Guid organizationId);
    Task<ServiceResult<bool>> IsItemReservedAsync(Guid itemId, Guid organizationId);

    // Reservation expiration management
    Task<ServiceResult<List<ReservationDto>>> GetExpiredReservationsAsync(Guid organizationId);
    Task<ServiceResult<int>> ProcessExpiredReservationsAsync(Guid organizationId);

    // Public catalog methods (for customer-facing reservation creation)
    Task<ServiceResult<ReservationDto>> CreatePublicReservationAsync(CreateReservationRequest request, Guid organizationId);
    Task<ServiceResult<ReservationDto>> GetPublicReservationAsync(Guid reservationId, string customerPhone);

    // Reservation completion (when customer picks up items)
    Task<ServiceResult<bool>> CompleteReservationAsync(Guid reservationId, Guid organizationId, List<Guid>? purchasedItemIds = null, Guid? userId = null);
    Task<ServiceResult<bool>> CancelReservationAsync(Guid reservationId, Guid organizationId, string? reason = null, Guid? userId = null);
}