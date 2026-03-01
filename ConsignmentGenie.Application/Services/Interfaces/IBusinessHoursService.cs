namespace ConsignmentGenie.Application.Services.Interfaces;

public interface IBusinessHoursService
{
    /// <summary>
    /// Calculate when a reservation should expire based on business hours
    /// If within 1 hour of close, extends to next business day
    /// </summary>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="reservationTime">When the reservation is made (default: now)</param>
    /// <returns>DateTime when reservation should expire</returns>
    Task<DateTime> CalculateReservationExpirationAsync(Guid organizationId, DateTime? reservationTime = null);

    /// <summary>
    /// Get the close of business time for today (or next business day if closed)
    /// </summary>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="forDate">Date to check (default: today)</param>
    /// <returns>Close of business DateTime</returns>
    Task<DateTime> GetCloseOfBusinessAsync(Guid organizationId, DateTime? forDate = null);

    /// <summary>
    /// Check if we're within 1 hour of closing time
    /// </summary>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="currentTime">Current time to check (default: now)</param>
    /// <returns>True if within 1 hour of close</returns>
    Task<bool> IsNearCloseOfBusinessAsync(Guid organizationId, DateTime? currentTime = null);
}