using ConsignmentGenie.Application.DTOs.Customer;
using ConsignmentGenie.Application.Interfaces;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace ConsignmentGenie.Application.Services;

public class CustomerAuthService : ICustomerAuthService
{
    private readonly ConsignmentGenieContext _context;
    private readonly ISmsService _smsService;
    private readonly IEmailService _emailService;
    private readonly ILogger<CustomerAuthService> _logger;

    public CustomerAuthService(
        ConsignmentGenieContext context,
        ISmsService smsService,
        IEmailService emailService,
        ILogger<CustomerAuthService> logger)
    {
        _context = context;
        _smsService = smsService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<CustomerVerificationResponse> StartVerificationAsync(CustomerVerificationRequest request, Guid organizationId)
    {
        _logger.LogInformation("[CUSTOMER_AUTH] Starting verification for {Email} using {Method}", request.Email, request.VerificationMethod);

        try
        {
            // Find or create customer
            var customer = await FindOrCreateCustomerAsync(request.Email, request.Name, organizationId);

            switch (request.VerificationMethod.ToLower())
            {
                case "google":
                    if (string.IsNullOrEmpty(request.OAuthToken))
                    {
                        return new CustomerVerificationResponse { Success = false, Message = "OAuth token required for Google verification" };
                    }
                    return await VerifyOAuthTokenAsync(request.OAuthToken, "google", organizationId);

                case "apple":
                    if (string.IsNullOrEmpty(request.OAuthToken))
                    {
                        return new CustomerVerificationResponse { Success = false, Message = "OAuth token required for Apple verification" };
                    }
                    return await VerifyOAuthTokenAsync(request.OAuthToken, "apple", organizationId);

                case "sms":
                    if (string.IsNullOrEmpty(request.PhoneNumber))
                    {
                        return new CustomerVerificationResponse { Success = false, Message = "Phone number required for SMS verification" };
                    }
                    return await StartSmsVerificationAsync(customer, request.PhoneNumber);

                case "email":
                    return await StartEmailVerificationAsync(customer);

                default:
                    return new CustomerVerificationResponse { Success = false, Message = "Unsupported verification method" };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CUSTOMER_AUTH] Failed to start verification for {Email}", request.Email);
            return new CustomerVerificationResponse { Success = false, Message = "Verification failed" };
        }
    }

    public async Task<CustomerVerificationResponse> CompleteVerificationAsync(string email, string code, string method, Guid organizationId)
    {
        _logger.LogInformation("[CUSTOMER_AUTH] Completing {Method} verification for {Email}", method, email);

        try
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Email.ToLower() == email.ToLower() && c.OrganizationId == organizationId);

            if (customer == null)
            {
                return new CustomerVerificationResponse { Success = false, Message = "Customer not found" };
            }

            bool isValid = method.ToLower() switch
            {
                "sms" => await ValidateSmsCodeAsync(customer, code),
                "email" => await ValidateEmailCodeAsync(customer, code),
                _ => false
            };

            if (!isValid)
            {
                return new CustomerVerificationResponse { Success = false, Message = "Invalid verification code" };
            }

            // Update customer verification status
            if (method.ToLower() == "sms" && !string.IsNullOrEmpty(customer.PhoneNumber))
            {
                // SMS verification complete - customer is verified via phone
            }

            await _context.SaveChangesAsync();

            var customerDto = await MapToCustomerDtoAsync(customer);

            _logger.LogInformation("[CUSTOMER_AUTH] Verification completed successfully for {Email}", email);
            return new CustomerVerificationResponse
            {
                Success = true,
                Message = "Verification completed",
                Customer = customerDto,
                SessionToken = GenerateSessionToken(customer.Id)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CUSTOMER_AUTH] Failed to complete verification for {Email}", email);
            return new CustomerVerificationResponse { Success = false, Message = "Verification failed" };
        }
    }

    public async Task<CustomerVerificationResponse> VerifyOAuthTokenAsync(string token, string provider, Guid organizationId)
    {
        _logger.LogInformation("[CUSTOMER_AUTH] Verifying {Provider} OAuth token", provider);

        try
        {
            // TODO: Implement actual OAuth token verification
            // For now, mock implementation that extracts email from token
            var (email, name, providerId) = await ValidateOAuthTokenAsync(token, provider);

            if (string.IsNullOrEmpty(email))
            {
                return new CustomerVerificationResponse { Success = false, Message = "Invalid OAuth token" };
            }

            var customer = await FindOrCreateCustomerAsync(email, name, organizationId);

            // Update OAuth ID
            if (provider.ToLower() == "google")
            {
                customer.GoogleId = providerId;
            }
            else if (provider.ToLower() == "apple")
            {
                customer.AppleId = providerId;
            }

            await _context.SaveChangesAsync();

            var customerDto = await MapToCustomerDtoAsync(customer);

            _logger.LogInformation("[CUSTOMER_AUTH] OAuth verification completed for {Email}", email);
            return new CustomerVerificationResponse
            {
                Success = true,
                Message = "Verification completed",
                Customer = customerDto,
                SessionToken = GenerateSessionToken(customer.Id)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CUSTOMER_AUTH] Failed OAuth verification with {Provider}", provider);
            return new CustomerVerificationResponse { Success = false, Message = "OAuth verification failed" };
        }
    }

    public async Task<CustomerDto?> GetCustomerAsync(Guid customerId, Guid organizationId)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == customerId && c.OrganizationId == organizationId);

        return customer != null ? await MapToCustomerDtoAsync(customer) : null;
    }

    public async Task<CustomerDto?> FindCustomerByEmailAsync(string email, Guid organizationId)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Email.ToLower() == email.ToLower() && c.OrganizationId == organizationId);

        return customer != null ? await MapToCustomerDtoAsync(customer) : null;
    }

    private async Task<Customer> FindOrCreateCustomerAsync(string email, string name, Guid organizationId)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Email.ToLower() == email.ToLower() && c.OrganizationId == organizationId);

        if (customer == null)
        {
            customer = new Customer
            {
                Email = email,
                Name = name,
                OrganizationId = organizationId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            _logger.LogInformation("[CUSTOMER_AUTH] Created new customer account for {Email}", email);
        }

        return customer;
    }

    private async Task<CustomerVerificationResponse> StartSmsVerificationAsync(Customer customer, string phoneNumber)
    {
        var code = GenerateVerificationCode();

        // Update customer with phone and code
        customer.PhoneNumber = phoneNumber;
        // Store verification code in reservation-specific fields for now
        // TODO: Add customer-specific verification fields if needed

        await _context.SaveChangesAsync();

        var smsResult = await _smsService.SendVerificationCodeAsync(phoneNumber, code, customer.Name);

        if (!smsResult.Success)
        {
            return new CustomerVerificationResponse { Success = false, Message = "Failed to send verification code" };
        }

        _logger.LogInformation("[CUSTOMER_AUTH] SMS verification code sent to {Phone}", phoneNumber);
        return new CustomerVerificationResponse
        {
            Success = true,
            Message = "Verification code sent",
            RequiresCodeVerification = true
        };
    }

    private async Task<CustomerVerificationResponse> StartEmailVerificationAsync(Customer customer)
    {
        var code = GenerateVerificationCode();

        // TODO: Store email verification code on customer
        // For now, mock implementation

        var sent = await _emailService.SendSimpleEmailAsync(
            customer.Email,
            "Your verification code",
            $"Your verification code is: {code}",
            isHtml: false);

        if (!sent)
        {
            return new CustomerVerificationResponse { Success = false, Message = "Failed to send verification code" };
        }

        _logger.LogInformation("[CUSTOMER_AUTH] Email verification code sent to {Email}", customer.Email);
        return new CustomerVerificationResponse
        {
            Success = true,
            Message = "Verification code sent",
            RequiresCodeVerification = true
        };
    }

    private async Task<bool> ValidateSmsCodeAsync(Customer customer, string code)
    {
        // TODO: Implement proper SMS code validation
        // For now, accept any 6-digit code for development
        return !string.IsNullOrEmpty(code) && code.Length == 6;
    }

    private async Task<bool> ValidateEmailCodeAsync(Customer customer, string code)
    {
        // TODO: Implement proper email code validation
        // For now, accept any 6-digit code for development
        return !string.IsNullOrEmpty(code) && code.Length == 6;
    }

    private async Task<(string email, string name, string providerId)> ValidateOAuthTokenAsync(string token, string provider)
    {
        // TODO: Implement actual OAuth token validation with Google/Apple APIs
        // For development, mock implementation that extracts info from token

        if (provider.ToLower() == "google" && token.StartsWith("google_"))
        {
            // Mock Google token format: google_email@example.com_name_googleid123
            var parts = token.Split('_');
            if (parts.Length >= 4)
            {
                return (parts[1], parts[2], parts[3]);
            }
        }
        else if (provider.ToLower() == "apple" && token.StartsWith("apple_"))
        {
            // Mock Apple token format: apple_email@example.com_name_appleid123
            var parts = token.Split('_');
            if (parts.Length >= 4)
            {
                return (parts[1], parts[2], parts[3]);
            }
        }

        return (string.Empty, string.Empty, string.Empty);
    }

    private async Task<CustomerDto> MapToCustomerDtoAsync(Customer customer)
    {
        var activeReservations = await _context.Reservations
            .Where(r => r.CustomerId == customer.Id && r.Status == Core.Enums.ReservationStatus.Confirmed)
            .CountAsync();

        var completedReservations = await _context.Reservations
            .Where(r => r.CustomerId == customer.Id && r.Status == Core.Enums.ReservationStatus.Completed)
            .CountAsync();

        return new CustomerDto
        {
            Id = customer.Id,
            Email = customer.Email,
            Name = customer.Name,
            PhoneNumber = customer.PhoneNumber,
            OrganizationId = customer.OrganizationId,
            HasGoogleAccount = !string.IsNullOrEmpty(customer.GoogleId),
            HasAppleAccount = !string.IsNullOrEmpty(customer.AppleId),
            IsVerified = customer.IsVerified,
            FirstReservationAt = customer.FirstReservationAt,
            CreatedAt = customer.CreatedAt,
            ActiveReservations = activeReservations,
            CompletedReservations = completedReservations
        };
    }

    private string GenerateVerificationCode()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[4];
        rng.GetBytes(bytes);
        var code = Math.Abs(BitConverter.ToInt32(bytes, 0)) % 1000000;
        return code.ToString("D6");
    }

    private string GenerateSessionToken(Guid customerId)
    {
        // TODO: Implement proper JWT token generation
        // For now, simple token format
        var tokenData = $"{customerId}_{DateTime.UtcNow.Ticks}";
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(tokenData));
    }
}