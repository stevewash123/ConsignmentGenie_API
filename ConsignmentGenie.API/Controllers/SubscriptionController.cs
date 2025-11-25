using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Owner")]
public class SubscriptionController : ControllerBase
{
    private readonly IStripeService _stripeService;
    private readonly IEmailService _emailService;

    public SubscriptionController(IStripeService stripeService, IEmailService emailService)
    {
        _stripeService = stripeService;
        _emailService = emailService;
    }

    private Guid GetOrganizationId()
    {
        var organizationIdClaim = User.FindFirst("OrganizationId")?.Value;
        return Guid.TryParse(organizationIdClaim, out var orgId) ? orgId : Guid.Empty;
    }

    [HttpGet("founder-status")]
    public async Task<ActionResult<ApiResponse<FounderEligibilityResult>>> GetFounderStatus()
    {
        try
        {
            var result = await _stripeService.ValidateFounderEligibilityAsync();
            return Ok(ApiResponse<FounderEligibilityResult>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<FounderEligibilityResult>.ErrorResult($"Failed to check founder status: {ex.Message}"));
        }
    }

    [HttpPost("create")]
    public async Task<ActionResult<ApiResponse<SubscriptionResult>>> CreateSubscription([FromBody] CreateSubscriptionRequest request)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var result = await _stripeService.CreateSubscriptionAsync(
                organizationId,
                request.Tier,
                request.IsFounder,
                request.FounderTier
            );

            return Ok(ApiResponse<SubscriptionResult>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<SubscriptionResult>.ErrorResult($"Failed to create subscription: {ex.Message}"));
        }
    }

    [HttpPost("update-tier")]
    public async Task<ActionResult<ApiResponse<SubscriptionResult>>> UpdateTier([FromBody] UpdateTierRequest request)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var result = await _stripeService.UpdateSubscriptionAsync(organizationId, request.NewTier);

            return Ok(ApiResponse<SubscriptionResult>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<SubscriptionResult>.ErrorResult($"Failed to update subscription: {ex.Message}"));
        }
    }

    [HttpPost("cancel")]
    public async Task<ActionResult<ApiResponse<bool>>> CancelSubscription([FromBody] CancelSubscriptionRequest request)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var result = await _stripeService.CancelSubscriptionAsync(organizationId, request.Immediately);

            return Ok(ApiResponse<bool>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<bool>.ErrorResult($"Failed to cancel subscription: {ex.Message}"));
        }
    }

    [HttpPost("billing-portal")]
    public async Task<ActionResult<ApiResponse<string>>> CreateBillingPortalSession([FromBody] BillingPortalRequest request)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var portalUrl = await _stripeService.CreateBillingPortalSessionAsync(organizationId, request.ReturnUrl);

            return Ok(ApiResponse<string>.SuccessResult(portalUrl));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<string>.ErrorResult($"Failed to create billing portal session: {ex.Message}"));
        }
    }

    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> HandleWebhook()
    {
        try
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var stripeSignature = Request.Headers["Stripe-Signature"].FirstOrDefault();

            if (string.IsNullOrEmpty(stripeSignature))
            {
                return BadRequest("Missing Stripe signature");
            }

            await _stripeService.ProcessWebhookAsync(json, stripeSignature);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest($"Webhook processing failed: {ex.Message}");
        }
    }
}

// DTOs for requests
public class CreateSubscriptionRequest
{
    public SubscriptionTier Tier { get; set; } = SubscriptionTier.Basic;
    public bool IsFounder { get; set; }
    public int? FounderTier { get; set; }
}

public class UpdateTierRequest
{
    public SubscriptionTier NewTier { get; set; }
}

public class CancelSubscriptionRequest
{
    public bool Immediately { get; set; } = false;
}

public class BillingPortalRequest
{
    public string ReturnUrl { get; set; } = string.Empty;
}