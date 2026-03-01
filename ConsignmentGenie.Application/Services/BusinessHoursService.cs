using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ConsignmentGenie.Application.Services;

public class BusinessHoursService : IBusinessHoursService
{
    private readonly ConsignmentGenieContext _context;
    private readonly ILogger<BusinessHoursService> _logger;

    public BusinessHoursService(ConsignmentGenieContext context, ILogger<BusinessHoursService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DateTime> CalculateReservationExpirationAsync(Guid organizationId, DateTime? reservationTime = null)
    {
        var reservationDateTime = reservationTime ?? DateTime.UtcNow;

        _logger.LogDebug("[BUSINESS_HOURS] Calculating reservation expiration for org {OrganizationId} at {ReservationTime}",
            organizationId, reservationDateTime);

        // Check if we're near close of business
        var isNearClose = await IsNearCloseOfBusinessAsync(organizationId, reservationDateTime);

        if (isNearClose)
        {
            // Extend to next business day
            var nextBusinessDay = await GetNextBusinessDayAsync(organizationId, reservationDateTime);
            var expiration = await GetCloseOfBusinessAsync(organizationId, nextBusinessDay);

            _logger.LogInformation("[BUSINESS_HOURS] Reservation made near close - extending to next business day: {ExpirationTime}", expiration);
            return expiration;
        }
        else
        {
            // Expire at end of today
            var expiration = await GetCloseOfBusinessAsync(organizationId, reservationDateTime.Date);

            _logger.LogDebug("[BUSINESS_HOURS] Reservation expires today at: {ExpirationTime}", expiration);
            return expiration;
        }
    }

    public async Task<DateTime> GetCloseOfBusinessAsync(Guid organizationId, DateTime? forDate = null)
    {
        var targetDate = forDate ?? DateTime.UtcNow;

        // TODO: For MVP, use simple 6 PM close time
        // In production, this would read from Organization.BusinessSettings JSON
        var closeTime = targetDate.Date.AddHours(18); // 6 PM

        _logger.LogDebug("[BUSINESS_HOURS] Close of business for {Date}: {CloseTime}", targetDate.Date, closeTime);
        return closeTime;
    }

    public async Task<bool> IsNearCloseOfBusinessAsync(Guid organizationId, DateTime? currentTime = null)
    {
        var now = currentTime ?? DateTime.UtcNow;
        var closeOfBusiness = await GetCloseOfBusinessAsync(organizationId, now);

        // Check if we're within 1 hour of closing
        var oneHourBeforeClose = closeOfBusiness.AddHours(-1);
        var isNearClose = now >= oneHourBeforeClose;

        _logger.LogDebug("[BUSINESS_HOURS] Current time: {Now}, Close: {Close}, Near close: {IsNearClose}",
            now, closeOfBusiness, isNearClose);

        return isNearClose;
    }

    private async Task<DateTime> GetNextBusinessDayAsync(Guid organizationId, DateTime fromDate)
    {
        // TODO: For MVP, assume Monday-Saturday are business days
        // In production, this would read from Organization.BusinessSettings JSON

        var nextDay = fromDate.AddDays(1);

        // Skip Sundays (simple implementation)
        while (nextDay.DayOfWeek == DayOfWeek.Sunday)
        {
            nextDay = nextDay.AddDays(1);
        }

        _logger.LogDebug("[BUSINESS_HOURS] Next business day after {FromDate}: {NextBusinessDay}", fromDate.Date, nextDay.Date);
        return nextDay;
    }
}