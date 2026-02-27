using ConsignmentGenie.Application.DTOs;

namespace ConsignmentGenie.Application.Interfaces;

public interface ISmsService
{
    /// <summary>
    /// Send SMS verification code to phone number
    /// </summary>
    /// <param name="phoneNumber">Phone number in E.164 format (e.g., +12345678900)</param>
    /// <param name="verificationCode">6-digit verification code</param>
    /// <param name="customerName">Customer name for personalization</param>
    /// <returns>ServiceResult indicating success or failure</returns>
    Task<ServiceResult<bool>> SendVerificationCodeAsync(string phoneNumber, string verificationCode, string customerName);

    /// <summary>
    /// Send reservation confirmation SMS
    /// </summary>
    /// <param name="phoneNumber">Phone number in E.164 format</param>
    /// <param name="customerName">Customer name</param>
    /// <param name="reservationId">Reservation ID</param>
    /// <param name="itemCount">Number of items reserved</param>
    /// <param name="totalValue">Total value of reserved items</param>
    /// <param name="expiresAt">When the reservation expires</param>
    /// <returns>ServiceResult indicating success or failure</returns>
    Task<ServiceResult<bool>> SendReservationConfirmationAsync(string phoneNumber, string customerName, Guid reservationId, int itemCount, decimal totalValue, DateTime expiresAt);

    /// <summary>
    /// Send reservation expiration reminder SMS
    /// </summary>
    /// <param name="phoneNumber">Phone number in E.164 format</param>
    /// <param name="customerName">Customer name</param>
    /// <param name="reservationId">Reservation ID</param>
    /// <param name="hoursRemaining">Hours until expiration</param>
    /// <returns>ServiceResult indicating success or failure</returns>
    Task<ServiceResult<bool>> SendExpirationReminderAsync(string phoneNumber, string customerName, Guid reservationId, int hoursRemaining);

    /// <summary>
    /// Format phone number to E.164 format
    /// </summary>
    /// <param name="phoneNumber">Input phone number in various formats</param>
    /// <param name="defaultCountryCode">Default country code if not specified (default: +1 for US)</param>
    /// <returns>Formatted phone number or null if invalid</returns>
    string? FormatPhoneNumber(string phoneNumber, string defaultCountryCode = "+1");

    /// <summary>
    /// Validate if phone number is properly formatted
    /// </summary>
    /// <param name="phoneNumber">Phone number to validate</param>
    /// <returns>True if valid E.164 format</returns>
    bool IsValidPhoneNumber(string phoneNumber);
}