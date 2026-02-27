using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/customer-invitations")]
[Authorize]
public class CustomerInvitationsController : ControllerBase
{
    private readonly ILogger<CustomerInvitationsController> _logger;

    public CustomerInvitationsController(
        ILogger<CustomerInvitationsController> logger)
    {
        _logger = logger;
    }

    private Guid GetOrganizationId()
    {
        // Use standard organizationId claim (lowercase following JWT conventions)
        var orgIdClaim = User.FindFirst("organizationId")?.Value;
        if (Guid.TryParse(orgIdClaim, out var orgId))
            return orgId;
        throw new UnauthorizedAccessException("Organization ID not found in token");
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
            return userId;
        throw new UnauthorizedAccessException("User ID not found in token");
    }

    /// <summary>
    /// Send customer invitations
    /// </summary>
    [HttpPost]
    public async Task<ActionResult> SendInvitations([FromBody] CustomerInvitationRequest request)
    {
        var organizationId = GetOrganizationId();
        var senderId = GetUserId();

        if (organizationId == Guid.Empty || senderId == Guid.Empty)
        {
            return Unauthorized("Invalid user context");
        }

        _logger.LogInformation("[CUSTOMER_INVITATION] Sending invitations to {EmailCount} recipients", request.Emails.Count);

        try
        {
            var response = new CustomerInvitationResponse
            {
                Success = true,
                Message = "Invitations sent successfully",
                InvitationsSent = request.Emails.Count,
                FailedEmails = new List<string>()
            };

            // For now, we'll simulate sending invitations
            // In the future, this would integrate with an email service
            foreach (var email in request.Emails)
            {
                try
                {
                    // TODO: Implement actual email sending logic
                    // This would send an email invitation to the customer
                    _logger.LogInformation("[CUSTOMER_INVITATION] Simulated sending invitation to {Email}", email);

                    // Simulate a small delay
                    await Task.Delay(100);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[CUSTOMER_INVITATION] Failed to send invitation to {Email}", email);
                    response.FailedEmails.Add(email);
                    response.InvitationsSent--;
                }
            }

            // Update response based on results
            if (response.FailedEmails.Count > 0)
            {
                if (response.InvitationsSent == 0)
                {
                    response.Success = false;
                    response.Message = "All invitations failed to send";
                }
                else
                {
                    response.Message = $"Sent {response.InvitationsSent} invitations. {response.FailedEmails.Count} failed.";
                }
            }

            return Ok(new { success = true, data = response });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CUSTOMER_INVITATION] Error sending customer invitations");
            return BadRequest(new { success = false, message = "Failed to send invitations" });
        }
    }

    /// <summary>
    /// Get invitation preview text
    /// </summary>
    [HttpPost("preview")]
    public async Task<ActionResult> GetInvitationPreview([FromBody] InvitationPreviewRequest request)
    {
        var organizationId = GetOrganizationId();

        if (organizationId == Guid.Empty)
        {
            return Unauthorized("Invalid organization context");
        }

        try
        {
            // TODO: Generate actual preview based on organization settings
            var previewText = GeneratePreviewText(request.PersonalMessage);

            return Ok(new { success = true, data = new { previewText = previewText } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CUSTOMER_INVITATION] Error generating preview");
            return BadRequest(new { success = false, message = "Failed to generate preview" });
        }
    }

    private string GeneratePreviewText(string personalMessage)
    {
        var baseText = "You're invited to shop our consignment store online!";

        if (!string.IsNullOrEmpty(personalMessage))
        {
            baseText = $"{personalMessage}\n\n{baseText}";
        }

        baseText += "\n\nClick the link below to browse our latest items and find great deals on quality consignment goods.";

        return baseText;
    }
}

// DTOs for the API
public class CustomerInvitationRequest
{
    public List<string> Emails { get; set; } = new();
    public string? PersonalMessage { get; set; }
    public bool IncludeStoreUrl { get; set; } = true;
    public string? CustomStoreUrl { get; set; }
}

public class CustomerInvitationResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int InvitationsSent { get; set; }
    public List<string> FailedEmails { get; set; } = new();
}

public class InvitationPreviewRequest
{
    public string PersonalMessage { get; set; } = string.Empty;
}