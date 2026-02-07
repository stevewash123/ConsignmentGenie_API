using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Application.DTOs.Account;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "owner")]
public class SubscriptionController : ControllerBase
{
    private readonly IStripeService _stripeService;
    private readonly IEmailService _emailService;
    private readonly IAccountService _accountService;

    public SubscriptionController(IStripeService stripeService, IEmailService emailService, IAccountService accountService)
    {
        _stripeService = stripeService;
        _emailService = emailService;
        _accountService = accountService;
    }

    private Guid GetOrganizationId()
    {
        var organizationIdClaim = User.FindFirst("organizationId")?.Value;
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

    [HttpGet]
    public async Task<ActionResult<SubscriptionInfoDto>> GetSubscriptionInfo()
    {
        try
        {
            var organizationId = GetOrganizationId();
            var accountInfo = await _accountService.GetAccountInfoAsync(organizationId);

            var subscriptionInfo = new SubscriptionInfoDto
            {
                PlanName = accountInfo.CurrentPlan.PlanType + (accountInfo.CurrentPlan.IsTrialActive ? " (Trial)" : ""),
                Status = accountInfo.CurrentPlan.IsTrialActive ? "trialing" : "active",
                NextBillingDate = accountInfo.CurrentPlan.NextBillingDate,
                CancelAtPeriodEnd = false, // TODO: Get from Stripe subscription
                TrialDaysRemaining = accountInfo.CurrentPlan.TrialDaysRemaining
            };

            return Ok(subscriptionInfo);
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to get subscription info: {ex.Message}");
        }
    }

    [HttpPost("portal")]
    public async Task<ActionResult<BillingPortalResponse>> CreateBillingPortalSession()
    {
        try
        {
            var organizationId = GetOrganizationId();
            var returnUrl = $"{Request.Scheme}://{Request.Host}/owner/settings/account/billing";
            var portalUrl = await _stripeService.CreateBillingPortalSessionAsync(organizationId, returnUrl);

            return Ok(new BillingPortalResponse { Url = portalUrl });
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to create billing portal session: {ex.Message}");
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

    [HttpGet("integration-pricing/{integration}")]
    public async Task<ActionResult<ApiResponse<IntegrationPricingResult>>> GetIntegrationPricing(string integration)
    {
        try
        {
            var organizationId = GetOrganizationId();
            if (organizationId == Guid.Empty)
            {
                return BadRequest(ApiResponse<IntegrationPricingResult>.ErrorResult("Organization ID not found in token"));
            }

            var result = await _stripeService.GetIntegrationPricingAsync(organizationId, integration);

            if (!result.Success)
            {
                return BadRequest(ApiResponse<IntegrationPricingResult>.ErrorResult(result.ErrorMessage ?? "Failed to get integration pricing"));
            }

            return Ok(ApiResponse<IntegrationPricingResult>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<IntegrationPricingResult>.ErrorResult($"Failed to get integration pricing: {ex.Message}"));
        }
    }

    [HttpGet("integrations")]
    public async Task<ActionResult<ApiResponse<IntegrationsInfoDto>>> GetAvailableIntegrations()
    {
        try
        {
            var organizationId = GetOrganizationId();
            if (organizationId == Guid.Empty)
            {
                return BadRequest(ApiResponse<IntegrationsInfoDto>.ErrorResult("Organization ID not found in token"));
            }

            var accountInfo = await _accountService.GetAccountInfoAsync(organizationId);

            var integrationsInfo = new IntegrationsInfoDto
            {
                ActiveIntegrations = accountInfo.CurrentPlan.ActiveIntegrations,
                AvailableIntegrations = GetAllAvailableIntegrations(),
                MaxIntegrations = 4, // QuickBooks, Stripe, Square POS, Dwolla
                IntegrationPrice = accountInfo.CurrentPlan.IntegrationPrice
            };

            return Ok(ApiResponse<IntegrationsInfoDto>.SuccessResult(integrationsInfo));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<IntegrationsInfoDto>.ErrorResult($"Failed to get integrations info: {ex.Message}"));
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

    private List<IntegrationDto> GetAllAvailableIntegrations()
    {
        return new List<IntegrationDto>
        {
            new IntegrationDto
            {
                Id = "quickbooks",
                Name = "QuickBooks Online",
                Type = "accounting",
                Description = "Sync transactions, items, and payouts with QuickBooks for seamless bookkeeping",
                IsActive = false,
                ConfigurationRequired = true,
                Status = "Available"
            },
            new IntegrationDto
            {
                Id = "stripe",
                Name = "Stripe Payments",
                Type = "payments",
                Description = "Accept credit cards and digital payments from customers",
                IsActive = false,
                ConfigurationRequired = false,
                Status = "Available"
            },
            new IntegrationDto
            {
                Id = "square-pos",
                Name = "Square POS",
                Type = "inventory",
                Description = "Sync inventory and sales with Square POS for in-store transactions",
                IsActive = false,
                ConfigurationRequired = true,
                Status = "Available"
            },
            new IntegrationDto
            {
                Id = "dwolla",
                Name = "Dwolla ACH",
                Type = "banking",
                Description = "Automate consignor payouts with bank account integration",
                IsActive = false,
                ConfigurationRequired = true,
                Status = "Available"
            }
        };
    }

    private string MapTierToPlanName(SubscriptionTier tier)
    {
        return tier switch
        {
            SubscriptionTier.Basic => "ConsignmentGenie Basic",
            SubscriptionTier.Pro => "ConsignmentGenie Pro",
            SubscriptionTier.Enterprise => "ConsignmentGenie Enterprise",
            _ => "Unknown Plan"
        };
    }

    private string MapSubscriptionStatus(SubscriptionStatus status)
    {
        return status switch
        {
            SubscriptionStatus.Active => "active",
            SubscriptionStatus.Cancelled => "cancelled",
            SubscriptionStatus.PastDue => "past_due",
            SubscriptionStatus.Trial => "active", // Show trial as active
            SubscriptionStatus.Suspended => "cancelled",
            _ => "active"
        };
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

public class SubscriptionInfoDto
{
    public string PlanName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? NextBillingDate { get; set; }
    public bool CancelAtPeriodEnd { get; set; }
    public int? TrialDaysRemaining { get; set; }
}

public class BillingPortalResponse
{
    public string Url { get; set; } = string.Empty;
}