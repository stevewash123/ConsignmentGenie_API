using ConsignmentGenie.Application.DTOs.Customer;

namespace ConsignmentGenie.Application.Services.Interfaces;

public interface ICustomerAuthService
{
    /// <summary>
    /// Start the verification process for a customer
    /// Creates customer account if first time, or finds existing
    /// </summary>
    /// <param name="request">Customer verification request</param>
    /// <param name="organizationId">Organization ID</param>
    /// <returns>Verification response with next steps</returns>
    Task<CustomerVerificationResponse> StartVerificationAsync(CustomerVerificationRequest request, Guid organizationId);

    /// <summary>
    /// Complete verification with a code (SMS or email)
    /// </summary>
    /// <param name="email">Customer email</param>
    /// <param name="code">Verification code</param>
    /// <param name="method">Verification method (sms or email)</param>
    /// <param name="organizationId">Organization ID</param>
    /// <returns>Verification response with customer data</returns>
    Task<CustomerVerificationResponse> CompleteVerificationAsync(string email, string code, string method, Guid organizationId);

    /// <summary>
    /// Verify OAuth token and create/find customer
    /// </summary>
    /// <param name="token">OAuth token from Google/Apple</param>
    /// <param name="provider">Provider (google or apple)</param>
    /// <param name="organizationId">Organization ID</param>
    /// <returns>Verification response with customer data</returns>
    Task<CustomerVerificationResponse> VerifyOAuthTokenAsync(string token, string provider, Guid organizationId);

    /// <summary>
    /// Get customer by ID
    /// </summary>
    /// <param name="customerId">Customer ID</param>
    /// <param name="organizationId">Organization ID</param>
    /// <returns>Customer DTO or null</returns>
    Task<CustomerDto?> GetCustomerAsync(Guid customerId, Guid organizationId);

    /// <summary>
    /// Find customer by email within organization
    /// </summary>
    /// <param name="email">Customer email</param>
    /// <param name="organizationId">Organization ID</param>
    /// <returns>Customer DTO or null</returns>
    Task<CustomerDto?> FindCustomerByEmailAsync(string email, Guid organizationId);
}