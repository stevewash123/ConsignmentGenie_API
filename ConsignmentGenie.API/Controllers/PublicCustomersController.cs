using Microsoft.AspNetCore.Mvc;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/public/customers")]
public class PublicCustomersController : ControllerBase
{
    private readonly ILogger<PublicCustomersController> _logger;

    public PublicCustomersController(ILogger<PublicCustomersController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Create a simple customer record for verified reservations
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateCustomer([FromBody] SimpleCustomerCreateRequest request)
    {
        // Simple customer creation for demo purposes
        // In production, this would go through proper customer verification service
        var customerId = Guid.NewGuid();

        try
        {
            // For now, return a mock response - in production this would create a real customer
            var response = new
            {
                success = true,
                data = new
                {
                    id = customerId.ToString(),
                    email = request.Email,
                    name = request.Name
                }
            };

            _logger.LogInformation("Demo customer created: {CustomerId} for {Email}", customerId, request.Email);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create customer for {Email}", request.Email);
            return BadRequest(new { success = false, message = "Failed to create customer account" });
        }
    }
}

public class SimpleCustomerCreateRequest
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string OrganizationId { get; set; } = string.Empty;
}