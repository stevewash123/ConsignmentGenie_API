using ConsignmentGenie.Application.DTOs.Customer;
using ConsignmentGenie.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerAuthService _customerAuthService;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(ICustomerAuthService customerAuthService, ILogger<CustomersController> logger)
    {
        _customerAuthService = customerAuthService;
        _logger = logger;
    }

    /// <summary>
    /// Start customer verification process
    /// </summary>
    [HttpPost("verify")]
    public async Task<ActionResult<CustomerVerificationResponse>> StartVerification(
        [FromBody] CustomerVerificationRequest request,
        [FromQuery] string storeCode)
    {
        if (string.IsNullOrEmpty(storeCode))
        {
            return BadRequest(new { success = false, message = "Store code is required" });
        }

        try
        {
            // TODO: Get organization ID from store code
            // For now, mock implementation
            var organizationId = await GetOrganizationIdFromStoreCodeAsync(storeCode);

            if (organizationId == Guid.Empty)
            {
                return BadRequest(new { success = false, message = "Invalid store code" });
            }

            var result = await _customerAuthService.StartVerificationAsync(request, organizationId);

            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CUSTOMERS] Failed to start verification for {Email}", request.Email);
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Complete verification with code (SMS or email)
    /// </summary>
    [HttpPost("verify/complete")]
    public async Task<ActionResult<CustomerVerificationResponse>> CompleteVerification(
        [FromBody] CompleteVerificationRequest request,
        [FromQuery] string storeCode)
    {
        if (string.IsNullOrEmpty(storeCode))
        {
            return BadRequest(new { success = false, message = "Store code is required" });
        }

        try
        {
            var organizationId = await GetOrganizationIdFromStoreCodeAsync(storeCode);

            if (organizationId == Guid.Empty)
            {
                return BadRequest(new { success = false, message = "Invalid store code" });
            }

            var result = await _customerAuthService.CompleteVerificationAsync(
                request.Email,
                request.Code,
                request.Method,
                organizationId);

            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CUSTOMERS] Failed to complete verification for {Email}", request.Email);
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get customer profile
    /// </summary>
    [HttpGet("profile")]
    public async Task<ActionResult<CustomerDto>> GetProfile(
        [FromQuery] string customerId,
        [FromQuery] string storeCode)
    {
        if (string.IsNullOrEmpty(storeCode) || string.IsNullOrEmpty(customerId))
        {
            return BadRequest(new { success = false, message = "Store code and customer ID are required" });
        }

        try
        {
            var organizationId = await GetOrganizationIdFromStoreCodeAsync(storeCode);

            if (organizationId == Guid.Empty)
            {
                return BadRequest(new { success = false, message = "Invalid store code" });
            }

            if (!Guid.TryParse(customerId, out var customerGuid))
            {
                return BadRequest(new { success = false, message = "Invalid customer ID" });
            }

            var customer = await _customerAuthService.GetCustomerAsync(customerGuid, organizationId);

            if (customer == null)
            {
                return NotFound(new { success = false, message = "Customer not found" });
            }

            return Ok(customer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CUSTOMERS] Failed to get customer profile for {CustomerId}", customerId);
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    private async Task<Guid> GetOrganizationIdFromStoreCodeAsync(string storeCode)
    {
        // TODO: Implement actual store code lookup
        // For now, return a mock organization ID
        return Guid.Parse("12345678-1234-1234-1234-123456789012");
    }
}

/// <summary>
/// Request to complete verification with a code
/// </summary>
public class CompleteVerificationRequest
{
    public string Email { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty; // "sms" or "email"
}