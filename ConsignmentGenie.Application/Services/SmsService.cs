using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Application.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace ConsignmentGenie.Application.Services;

/// <summary>
/// SMS service implementation using Twilio (stub implementation for now)
/// TODO: Integrate with actual Twilio SDK when SMS verification is activated
/// </summary>
public class SmsService : ISmsService
{
    private readonly ILogger<SmsService> _logger;
    private readonly bool _isEnabled;

    public SmsService(ILogger<SmsService> logger)
    {
        _logger = logger;
        // TODO: Get this from configuration when SMS is enabled
        _isEnabled = false; // Disabled by default until properly configured
    }

    public async Task<ServiceResult<bool>> SendVerificationCodeAsync(string phoneNumber, string verificationCode, string customerName)
    {
        try
        {
            if (!_isEnabled)
            {
                _logger.LogInformation("SMS disabled - would send verification code {Code} to {Phone} for {Customer}",
                    verificationCode, phoneNumber, customerName);
                return ServiceResult<bool>.SuccessResult(true);
            }

            // TODO: Implement actual Twilio integration
            var message = $"Hi {customerName}! Your ConsignmentGenie verification code is: {verificationCode}. This code expires in 15 minutes.";

            _logger.LogInformation("Sending SMS verification code to {Phone}: {Message}", phoneNumber, message);

            // Simulate SMS sending delay
            await Task.Delay(100);

            return ServiceResult<bool>.SuccessResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send verification code SMS to {Phone}", phoneNumber);
            return ServiceResult<bool>.FailureResult("Failed to send verification code");
        }
    }

    public async Task<ServiceResult<bool>> SendReservationConfirmationAsync(string phoneNumber, string customerName, Guid reservationId, int itemCount, decimal totalValue, DateTime expiresAt)
    {
        try
        {
            if (!_isEnabled)
            {
                _logger.LogInformation("SMS disabled - would send reservation confirmation to {Phone} for {Customer}, reservation {ReservationId}",
                    phoneNumber, customerName, reservationId);
                return ServiceResult<bool>.SuccessResult(true);
            }

            // TODO: Implement actual Twilio integration
            var expiresIn = (expiresAt - DateTime.UtcNow).TotalHours;
            var message = $"Hi {customerName}! Your reservation for {itemCount} item(s) totaling ${totalValue:F2} is confirmed. " +
                         $"Items are held until {expiresAt:MMM d 'at' h:mm tt} ({expiresIn:F0} hours). " +
                         $"Reservation ID: {reservationId.ToString()[..8]}";

            _logger.LogInformation("Sending SMS reservation confirmation to {Phone}: {Message}", phoneNumber, message);

            // Simulate SMS sending delay
            await Task.Delay(100);

            return ServiceResult<bool>.SuccessResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send reservation confirmation SMS to {Phone}", phoneNumber);
            return ServiceResult<bool>.FailureResult("Failed to send confirmation message");
        }
    }

    public async Task<ServiceResult<bool>> SendExpirationReminderAsync(string phoneNumber, string customerName, Guid reservationId, int hoursRemaining)
    {
        try
        {
            if (!_isEnabled)
            {
                _logger.LogInformation("SMS disabled - would send expiration reminder to {Phone} for {Customer}, reservation {ReservationId}, {Hours}h remaining",
                    phoneNumber, customerName, reservationId, hoursRemaining);
                return ServiceResult<bool>.SuccessResult(true);
            }

            // TODO: Implement actual Twilio integration
            var message = $"Hi {customerName}! Reminder: Your reserved items expire in {hoursRemaining} hour(s). " +
                         $"Please pick them up soon to avoid losing your hold. " +
                         $"Reservation ID: {reservationId.ToString()[..8]}";

            _logger.LogInformation("Sending SMS expiration reminder to {Phone}: {Message}", phoneNumber, message);

            // Simulate SMS sending delay
            await Task.Delay(100);

            return ServiceResult<bool>.SuccessResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send expiration reminder SMS to {Phone}", phoneNumber);
            return ServiceResult<bool>.FailureResult("Failed to send reminder message");
        }
    }

    public string? FormatPhoneNumber(string phoneNumber, string defaultCountryCode = "+1")
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return null;

        // Remove all non-digit characters
        var digits = Regex.Replace(phoneNumber, @"[^\d]", "");

        // Handle different input formats
        if (digits.Length == 10)
        {
            // US format without country code
            return $"{defaultCountryCode}{digits}";
        }
        else if (digits.Length == 11 && digits.StartsWith("1"))
        {
            // US format with country code but no +
            return $"+{digits}";
        }
        else if (phoneNumber.StartsWith("+") && digits.Length >= 10)
        {
            // Already in international format
            return phoneNumber;
        }

        // Invalid format
        return null;
    }

    public bool IsValidPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        // Basic E.164 format validation
        var e164Pattern = @"^\+[1-9]\d{1,14}$";
        return Regex.IsMatch(phoneNumber, e164Pattern);
    }
}

/// <summary>
/// Console-based SMS service for development/testing
/// Logs SMS messages to console instead of actually sending them
/// </summary>
public class ConsoleSmsService : ISmsService
{
    private readonly ILogger<ConsoleSmsService> _logger;

    public ConsoleSmsService(ILogger<ConsoleSmsService> logger)
    {
        _logger = logger;
    }

    public Task<ServiceResult<bool>> SendVerificationCodeAsync(string phoneNumber, string verificationCode, string customerName)
    {
        Console.WriteLine();
        Console.WriteLine("=== SMS VERIFICATION CODE ===");
        Console.WriteLine($"TO: {phoneNumber}");
        Console.WriteLine($"CUSTOMER: {customerName}");
        Console.WriteLine($"CODE: {verificationCode}");
        Console.WriteLine($"MESSAGE: Hi {customerName}! Your ConsignmentGenie verification code is: {verificationCode}. This code expires in 15 minutes.");
        Console.WriteLine("==============================");
        Console.WriteLine();

        _logger.LogInformation("Console SMS: Verification code {Code} sent to {Phone} for {Customer}",
            verificationCode, phoneNumber, customerName);

        return Task.FromResult(ServiceResult<bool>.SuccessResult(true));
    }

    public Task<ServiceResult<bool>> SendReservationConfirmationAsync(string phoneNumber, string customerName, Guid reservationId, int itemCount, decimal totalValue, DateTime expiresAt)
    {
        var expiresIn = (expiresAt - DateTime.UtcNow).TotalHours;

        Console.WriteLine();
        Console.WriteLine("=== SMS RESERVATION CONFIRMATION ===");
        Console.WriteLine($"TO: {phoneNumber}");
        Console.WriteLine($"CUSTOMER: {customerName}");
        Console.WriteLine($"RESERVATION: {reservationId}");
        Console.WriteLine($"MESSAGE: Hi {customerName}! Your reservation for {itemCount} item(s) totaling ${totalValue:F2} is confirmed. " +
                         $"Items are held until {expiresAt:MMM d 'at' h:mm tt} ({expiresIn:F0} hours). " +
                         $"Reservation ID: {reservationId.ToString()[..8]}");
        Console.WriteLine("====================================");
        Console.WriteLine();

        _logger.LogInformation("Console SMS: Reservation confirmation sent to {Phone} for {Customer}, reservation {ReservationId}",
            phoneNumber, customerName, reservationId);

        return Task.FromResult(ServiceResult<bool>.SuccessResult(true));
    }

    public Task<ServiceResult<bool>> SendExpirationReminderAsync(string phoneNumber, string customerName, Guid reservationId, int hoursRemaining)
    {
        Console.WriteLine();
        Console.WriteLine("=== SMS EXPIRATION REMINDER ===");
        Console.WriteLine($"TO: {phoneNumber}");
        Console.WriteLine($"CUSTOMER: {customerName}");
        Console.WriteLine($"RESERVATION: {reservationId}");
        Console.WriteLine($"MESSAGE: Hi {customerName}! Reminder: Your reserved items expire in {hoursRemaining} hour(s). " +
                         $"Please pick them up soon to avoid losing your hold. " +
                         $"Reservation ID: {reservationId.ToString()[..8]}");
        Console.WriteLine("===============================");
        Console.WriteLine();

        _logger.LogInformation("Console SMS: Expiration reminder sent to {Phone} for {Customer}, reservation {ReservationId}",
            phoneNumber, customerName, reservationId);

        return Task.FromResult(ServiceResult<bool>.SuccessResult(true));
    }

    public string? FormatPhoneNumber(string phoneNumber, string defaultCountryCode = "+1")
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return null;

        // Remove all non-digit characters
        var digits = Regex.Replace(phoneNumber, @"[^\d]", "");

        // Handle different input formats
        if (digits.Length == 10)
        {
            // US format without country code
            return $"{defaultCountryCode}{digits}";
        }
        else if (digits.Length == 11 && digits.StartsWith("1"))
        {
            // US format with country code but no +
            return $"+{digits}";
        }
        else if (phoneNumber.StartsWith("+") && digits.Length >= 10)
        {
            // Already in international format
            return phoneNumber;
        }

        // Invalid format
        return null;
    }

    public bool IsValidPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        // Basic E.164 format validation
        var e164Pattern = @"^\+[1-9]\d{1,14}$";
        return Regex.IsMatch(phoneNumber, e164Pattern);
    }
}