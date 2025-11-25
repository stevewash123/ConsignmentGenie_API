using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;

namespace ConsignmentGenie.Application.Services;

public class StripeService : IStripeService
{
    private readonly ConsignmentGenieContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<StripeService> _logger;

    public StripeService(ConsignmentGenieContext context, IConfiguration configuration, ILogger<StripeService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
    }

    public async Task<string> CreateCustomerAsync(Guid organizationId, string email, string organizationName)
    {
        // 🏗️ AGGREGATE ROOT PATTERN: Detach all tracked entities to avoid conflicts
        foreach (var entry in _context.ChangeTracker.Entries().ToList())
        {
            entry.State = EntityState.Detached;
        }

        var customerService = new CustomerService();

        var options = new CustomerCreateOptions
        {
            Email = email,
            Name = organizationName,
            Metadata = new Dictionary<string, string>
            {
                ["OrganizationId"] = organizationId.ToString()
            }
        };

        var customer = await customerService.CreateAsync(options);

        // 🏗️ AGGREGATE ROOT PATTERN: Update organization aggregate with Stripe customer ID
        var organization = await _context.Organizations.FindAsync(organizationId);
        if (organization != null)
        {
            organization.StripeCustomerId = customer.Id;
            await _context.SaveChangesAsync();
        }

        return customer.Id;
    }

    public async Task<SubscriptionResult> CreateSubscriptionAsync(Guid organizationId, SubscriptionTier tier, bool isFounder, int? founderTier)
    {
        try
        {
            var organization = await _context.Organizations.FindAsync(organizationId);
            if (organization == null)
            {
                return new SubscriptionResult { Success = false, ErrorMessage = "Organization not found" };
            }

            // Determine price based on tier and founder status
            var priceId = GetPriceId(tier, isFounder, founderTier);

            var sessionOptions = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                Mode = "subscription",
                Customer = organization.StripeCustomerId,
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Price = priceId,
                        Quantity = 1,
                    },
                },
                SuccessUrl = _configuration["Stripe:SuccessUrl"] + "?session_id={CHECKOUT_SESSION_ID}",
                CancelUrl = _configuration["Stripe:CancelUrl"],
                SubscriptionData = new SessionSubscriptionDataOptions
                {
                    TrialPeriodDays = 14,  // 14-day free trial
                    Metadata = new Dictionary<string, string>
                    {
                        ["OrganizationId"] = organizationId.ToString(),
                        ["Tier"] = tier.ToString(),
                        ["IsFounder"] = isFounder.ToString(),
                        ["FounderTier"] = founderTier?.ToString() ?? ""
                    }
                }
            };

            var sessionService = new SessionService();
            var session = await sessionService.CreateAsync(sessionOptions);

            return new SubscriptionResult
            {
                Success = true,
                ClientSecret = session.ClientSecret,
                CustomerId = organization.StripeCustomerId
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error creating subscription for organization {OrganizationId}", organizationId);
            return new SubscriptionResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<SubscriptionResult> UpdateSubscriptionAsync(Guid organizationId, SubscriptionTier newTier)
    {
        try
        {
            var organization = await _context.Organizations.FindAsync(organizationId);
            if (organization == null || string.IsNullOrEmpty(organization.StripeSubscriptionId))
            {
                return new SubscriptionResult { Success = false, ErrorMessage = "Active subscription not found" };
            }

            var subscriptionService = new SubscriptionService();
            var subscription = await subscriptionService.GetAsync(organization.StripeSubscriptionId);

            var newPriceId = GetPriceId(newTier, organization.IsFounderPricing, organization.FounderTier);

            var options = new SubscriptionUpdateOptions
            {
                Items = new List<SubscriptionItemOptions>
                {
                    new SubscriptionItemOptions
                    {
                        Id = subscription.Items.Data[0].Id,
                        Price = newPriceId,
                    },
                },
                ProrationBehavior = "create_prorations",  // Immediate upgrade charge
            };

            await subscriptionService.UpdateAsync(organization.StripeSubscriptionId, options);

            // Update organization tier
            organization.SubscriptionTier = newTier;
            organization.StripePriceId = newPriceId;
            await _context.SaveChangesAsync();

            return new SubscriptionResult { Success = true, SubscriptionId = organization.StripeSubscriptionId };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error updating subscription for organization {OrganizationId}", organizationId);
            return new SubscriptionResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<bool> CancelSubscriptionAsync(Guid organizationId, bool immediately = false)
    {
        try
        {
            var organization = await _context.Organizations.FindAsync(organizationId);
            if (organization == null || string.IsNullOrEmpty(organization.StripeSubscriptionId))
            {
                return false;
            }

            var subscriptionService = new SubscriptionService();

            if (immediately)
            {
                await subscriptionService.CancelAsync(organization.StripeSubscriptionId);
            }
            else
            {
                var options = new SubscriptionUpdateOptions
                {
                    CancelAtPeriodEnd = true,
                };
                await subscriptionService.UpdateAsync(organization.StripeSubscriptionId, options);
            }

            return true;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error canceling subscription for organization {OrganizationId}", organizationId);
            return false;
        }
    }

    public async Task<string> CreateBillingPortalSessionAsync(Guid organizationId, string returnUrl)
    {
        var organization = await _context.Organizations.FindAsync(organizationId);
        if (organization == null || string.IsNullOrEmpty(organization.StripeCustomerId))
        {
            throw new InvalidOperationException("Customer not found");
        }

        var options = new Stripe.BillingPortal.SessionCreateOptions
        {
            Customer = organization.StripeCustomerId,
            ReturnUrl = returnUrl,
        };

        var service = new Stripe.BillingPortal.SessionService();
        var session = await service.CreateAsync(options);

        return session.Url;
    }

    public async Task ProcessWebhookAsync(string json, string stripeSignature)
    {
        var webhookSecret = _configuration["Stripe:WebhookSecret"];
        Event stripeEvent;

        try
        {
            stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, webhookSecret);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify webhook signature");
            throw;
        }

        // Log the event
        await LogWebhookEventAsync(stripeEvent, json);

        // Process specific event types
        switch (stripeEvent.Type)
        {
            case Events.CustomerSubscriptionCreated:
                await HandleSubscriptionCreatedAsync(stripeEvent);
                break;
            case Events.CustomerSubscriptionUpdated:
                await HandleSubscriptionUpdatedAsync(stripeEvent);
                break;
            case Events.CustomerSubscriptionDeleted:
                await HandleSubscriptionDeletedAsync(stripeEvent);
                break;
            case Events.InvoicePaymentSucceeded:
                await HandlePaymentSucceededAsync(stripeEvent);
                break;
            case Events.InvoicePaymentFailed:
                await HandlePaymentFailedAsync(stripeEvent);
                break;
            default:
                _logger.LogInformation("Unhandled webhook event type: {EventType}", stripeEvent.Type);
                break;
        }
    }

    public async Task<FounderEligibilityResult> ValidateFounderEligibilityAsync()
    {
        var founderCount = await _context.Organizations
            .CountAsync(o => o.IsFounderPricing);

        if (founderCount < 10)
        {
            return new FounderEligibilityResult
            {
                IsEligible = true,
                FounderTier = 1,
                FounderPrice = 39m,
                Message = $"Founder Tier 1: $39/month forever! ({10 - founderCount} spots remaining)"
            };
        }
        else if (founderCount < 30)
        {
            return new FounderEligibilityResult
            {
                IsEligible = true,
                FounderTier = 2,
                FounderPrice = 59m,
                Message = $"Founder Tier 2: $59/month forever! ({30 - founderCount} spots remaining)"
            };
        }
        else if (founderCount < 50)
        {
            return new FounderEligibilityResult
            {
                IsEligible = true,
                FounderTier = 3,
                FounderPrice = 79m,
                Message = $"Founder Tier 3: $79/month forever! ({50 - founderCount} spots remaining)"
            };
        }
        else
        {
            return new FounderEligibilityResult
            {
                IsEligible = false,
                Message = "Founder pricing no longer available"
            };
        }
    }

    private string GetPriceId(SubscriptionTier tier, bool isFounder, int? founderTier)
    {
        // In production, these would be actual Stripe price IDs from your Stripe dashboard
        var basePath = "Stripe:Prices:";

        if (isFounder)
        {
            return tier switch
            {
                SubscriptionTier.Basic => founderTier switch
                {
                    1 => _configuration[$"{basePath}FounderTier1"] ?? "price_founder_tier_1_basic_39",
                    2 => _configuration[$"{basePath}FounderTier2"] ?? "price_founder_tier_2_basic_59",
                    3 => _configuration[$"{basePath}FounderTier3"] ?? "price_founder_tier_3_basic_79",
                    _ => _configuration[$"{basePath}Basic"] ?? "price_basic_79"
                },
                _ => _configuration[$"{basePath}Basic"] ?? "price_basic_79"
            };
        }

        return tier switch
        {
            SubscriptionTier.Basic => _configuration[$"{basePath}Basic"] ?? "price_basic_79",
            SubscriptionTier.Pro => _configuration[$"{basePath}Pro"] ?? "price_pro_129",
            SubscriptionTier.Enterprise => _configuration[$"{basePath}Enterprise"] ?? "price_enterprise_229",
            _ => _configuration[$"{basePath}Basic"] ?? "price_basic_79"
        };
    }

    private async Task LogWebhookEventAsync(Event stripeEvent, string json)
    {
        // Extract organization ID from event metadata
        Guid? organizationId = null;
        if (stripeEvent.Data.Object is Subscription subscription)
        {
            if (subscription.Metadata?.TryGetValue("OrganizationId", out var orgIdString) == true)
            {
                Guid.TryParse(orgIdString, out var orgId);
                organizationId = orgId;
            }
        }

        if (!organizationId.HasValue) return;

        var subscriptionEvent = new SubscriptionEvent
        {
            OrganizationId = organizationId.Value,
            EventType = stripeEvent.Type,
            StripeEventId = stripeEvent.Id,
            RawJson = json,
            Processed = false
        };

        _context.SubscriptionEvents.Add(subscriptionEvent);
        await _context.SaveChangesAsync();
    }

    private async Task HandleSubscriptionCreatedAsync(Event stripeEvent)
    {
        var subscription = stripeEvent.Data.Object as Subscription;
        if (subscription == null) return;

        var organizationId = GetOrganizationIdFromMetadata(subscription.Metadata);

        if (organizationId.HasValue)
        {
            var organization = await _context.Organizations.FindAsync(organizationId);
            if (organization != null)
            {
                organization.StripeSubscriptionId = subscription.Id;
                organization.SubscriptionStatus = SubscriptionStatus.Trial;
                organization.SubscriptionStartDate = DateTime.UtcNow;
                organization.SubscriptionEndDate = subscription.CurrentPeriodEnd;

                await _context.SaveChangesAsync();
            }
        }
    }

    private async Task HandleSubscriptionUpdatedAsync(Event stripeEvent)
    {
        var subscription = stripeEvent.Data.Object as Subscription;
        if (subscription == null) return;

        var organizationId = GetOrganizationIdFromMetadata(subscription.Metadata);

        if (organizationId.HasValue)
        {
            var organization = await _context.Organizations.FindAsync(organizationId);
            if (organization != null)
            {
                organization.SubscriptionStatus = subscription.Status switch
                {
                    "active" => SubscriptionStatus.Active,
                    "trialing" => SubscriptionStatus.Trial,
                    "past_due" => SubscriptionStatus.PastDue,
                    "canceled" => SubscriptionStatus.Cancelled,
                    _ => organization.SubscriptionStatus
                };
                organization.SubscriptionEndDate = subscription.CurrentPeriodEnd;

                await _context.SaveChangesAsync();
            }
        }
    }

    private async Task HandleSubscriptionDeletedAsync(Event stripeEvent)
    {
        var subscription = stripeEvent.Data.Object as Subscription;
        var organizationId = GetOrganizationIdFromMetadata(subscription?.Metadata);

        if (organizationId.HasValue)
        {
            var organization = await _context.Organizations.FindAsync(organizationId);
            if (organization != null)
            {
                organization.SubscriptionStatus = SubscriptionStatus.Cancelled;
                await _context.SaveChangesAsync();
            }
        }
    }

    private async Task HandlePaymentSucceededAsync(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        var subscriptionId = invoice?.SubscriptionId;

        if (!string.IsNullOrEmpty(subscriptionId))
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.StripeSubscriptionId == subscriptionId);

            if (organization != null)
            {
                organization.SubscriptionStatus = SubscriptionStatus.Active;
                await _context.SaveChangesAsync();
            }
        }
    }

    private async Task HandlePaymentFailedAsync(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        var subscriptionId = invoice?.SubscriptionId;

        if (!string.IsNullOrEmpty(subscriptionId))
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.StripeSubscriptionId == subscriptionId);

            if (organization != null)
            {
                organization.SubscriptionStatus = SubscriptionStatus.PastDue;
                await _context.SaveChangesAsync();

                // TODO: Send payment failed email notification
            }
        }
    }

    private Guid? GetOrganizationIdFromMetadata(IDictionary<string, string>? metadata)
    {
        if (metadata?.TryGetValue("OrganizationId", out var orgIdString) == true)
        {
            if (Guid.TryParse(orgIdString, out var orgId))
            {
                return orgId;
            }
        }
        return null;
    }
}