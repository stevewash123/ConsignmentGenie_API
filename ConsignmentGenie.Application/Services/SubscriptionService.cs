using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;
using Stripe.BillingPortal;

namespace ConsignmentGenie.Application.Services;

public interface ISubscriptionService
{
    Task<(bool isEligible, int? tier)> CheckFounderEligibilityAsync();
    Task<(string customerId, string subscriptionId)> CreateTrialSubscriptionAsync(Guid organizationId, string email, string organizationName);
    Task<string> CreateCheckoutSessionAsync(Guid organizationId, string[] integrations);
    Task<string> CreatePaymentMethodSetupAsync(Guid organizationId, string[] integrations);
    Task<bool> AddIntegrationAsync(Guid organizationId, string integrationKey);
    Task<bool> RemoveIntegrationAsync(Guid organizationId, string integrationKey);
    Task<string> GetCustomerPortalUrlAsync(Guid organizationId);
    Task<Subscription?> GetSubscriptionAsync(string subscriptionId);
    Task<Price[]> GetPricesAsync();
}

public class SubscriptionService : ISubscriptionService
{
    private readonly ConsignmentGenieContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SubscriptionService> _logger;
    private readonly CustomerService _customerService;
    private readonly Stripe.SubscriptionService _stripeSubscriptionService;
    private readonly Stripe.Checkout.SessionService _checkoutSessionService;
    private readonly Stripe.BillingPortal.SessionService _billingPortalService;
    private readonly PriceService _priceService;

    public SubscriptionService(
        ConsignmentGenieContext context,
        IConfiguration configuration,
        ILogger<SubscriptionService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;

        // Configure Stripe
        StripeConfiguration.ApiKey = configuration["Stripe:SecretKey"];

        _customerService = new CustomerService();
        _stripeSubscriptionService = new Stripe.SubscriptionService();
        _checkoutSessionService = new Stripe.Checkout.SessionService();
        _billingPortalService = new Stripe.BillingPortal.SessionService();
        _priceService = new PriceService();
    }

    public async Task<(bool isEligible, int? tier)> CheckFounderEligibilityAsync()
    {
        try
        {
            // Count subscriptions with founder pricing (trialing or active)
            var founderCount = await _context.Organizations
                .CountAsync(o => o.IsFounderPricing);

            if (founderCount < 10)
                return (true, 1);   // Founder 1: slots 1-10
            else if (founderCount < 30)
                return (true, 2);   // Founder 2: slots 11-30
            else
                return (false, null);  // No more founder slots
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking founder eligibility");
            return (false, null);
        }
    }

    public async Task<(string customerId, string subscriptionId)> CreateTrialSubscriptionAsync(
        Guid organizationId, string email, string organizationName)
    {
        try
        {
            // 1. Check founder eligibility
            var (isEligible, tier) = await CheckFounderEligibilityAsync();
            var basePriceKey = isEligible ? $"base_founder{tier}" : "base_regular";

            // 2. Create Stripe customer
            var customerOptions = new CustomerCreateOptions
            {
                Email = email,
                Name = organizationName,
                Metadata = new Dictionary<string, string>
                {
                    ["organization_id"] = organizationId.ToString()
                }
            };

            var customer = await _customerService.CreateAsync(customerOptions);

            // 3. Get the base price ID
            var priceId = await GetPriceIdByLookupKeyAsync(basePriceKey);
            if (string.IsNullOrEmpty(priceId))
            {
                throw new InvalidOperationException($"Price not found for lookup key: {basePriceKey}");
            }

            // 4. Create subscription with 30-day trial (no payment method required)
            var subscriptionOptions = new SubscriptionCreateOptions
            {
                Customer = customer.Id,
                Items = new List<SubscriptionItemOptions>
                {
                    new SubscriptionItemOptions { Price = priceId }
                },
                TrialPeriodDays = 30,
                PaymentSettings = new SubscriptionPaymentSettingsOptions
                {
                    SaveDefaultPaymentMethod = "on_subscription"
                },
                TrialSettings = new SubscriptionTrialSettingsOptions
                {
                    EndBehavior = new SubscriptionTrialSettingsEndBehaviorOptions
                    {
                        MissingPaymentMethod = "cancel"
                    }
                },
                Metadata = new Dictionary<string, string>
                {
                    ["organization_id"] = organizationId.ToString(),
                    ["founder_tier"] = tier?.ToString() ?? "",
                    ["is_founder"] = isEligible.ToString()
                }
            };

            var subscription = await _stripeSubscriptionService.CreateAsync(subscriptionOptions);

            // 5. Update organization record
            var org = await _context.Organizations.FindAsync(organizationId);
            if (org != null)
            {
                org.StripeCustomerId = customer.Id;
                org.StripeSubscriptionId = subscription.Id;
                org.IsFounderPricing = isEligible;
                org.FounderTier = tier;
                org.CachedSubscriptionStatus = subscription.Status;
                await _context.SaveChangesAsync();
            }

            return (customer.Id, subscription.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating trial subscription for organization {OrganizationId}", organizationId);
            throw;
        }
    }

    public async Task<string> CreateCheckoutSessionAsync(Guid organizationId, string[] integrations)
    {
        try
        {
            var org = await _context.Organizations.FindAsync(organizationId);
            if (org?.StripeCustomerId == null)
            {
                throw new InvalidOperationException("Organization does not have a Stripe customer");
            }

            // Build line items - base is already on subscription, add integrations
            var lineItems = new List<SessionLineItemOptions>();
            var priceSuffix = org.IsFounderPricing ? "founder" : "regular";

            foreach (var integration in integrations)
            {
                var priceId = await GetPriceIdByLookupKeyAsync($"{integration}_{priceSuffix}");
                if (!string.IsNullOrEmpty(priceId))
                {
                    lineItems.Add(new SessionLineItemOptions
                    {
                        Price = priceId,
                        Quantity = 1
                    });
                }
            }

            var appUrl = _configuration["App:BaseUrl"] ?? "http://localhost:4200";
            var options = new Stripe.Checkout.SessionCreateOptions
            {
                Customer = org.StripeCustomerId,
                LineItems = lineItems,
                Mode = "subscription",
                SuccessUrl = $"{appUrl}/settings/subscription?success=true",
                CancelUrl = $"{appUrl}/settings/subscription?canceled=true"
            };

            var session = await _checkoutSessionService.CreateAsync(options);
            return session.Url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating checkout session for organization {OrganizationId}", organizationId);
            throw;
        }
    }

    public async Task<string> CreatePaymentMethodSetupAsync(Guid organizationId, string[] integrations)
    {
        try
        {
            var org = await _context.Organizations.FindAsync(organizationId);
            if (org?.StripeCustomerId == null)
            {
                throw new InvalidOperationException("Organization does not have a Stripe customer");
            }

            var appUrl = _configuration["App:BaseUrl"] ?? "http://localhost:4200";
            var options = new Stripe.Checkout.SessionCreateOptions
            {
                Customer = org.StripeCustomerId,
                Mode = "setup",
                SuccessUrl = $"{appUrl}/settings/subscription?success=true",
                CancelUrl = $"{appUrl}/settings/subscription?canceled=true",
                SetupIntentData = new Stripe.Checkout.SessionSetupIntentDataOptions
                {
                    Metadata = new Dictionary<string, string>
                    {
                        ["subscription_id"] = org.StripeSubscriptionId!,
                        ["integrations_to_add"] = string.Join(",", integrations)
                    }
                }
            };

            var session = await _checkoutSessionService.CreateAsync(options);
            return session.Url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment setup session for organization {OrganizationId}", organizationId);
            throw;
        }
    }

    public async Task<bool> AddIntegrationAsync(Guid organizationId, string integrationKey)
    {
        try
        {
            var org = await _context.Organizations.FindAsync(organizationId);
            if (org?.StripeSubscriptionId == null) return false;

            var subscription = await _stripeSubscriptionService.GetAsync(org.StripeSubscriptionId);
            if (subscription == null) return false;

            // Determine correct price based on founder status
            var priceSuffix = org.IsFounderPricing ? "founder" : "regular";
            var priceId = await GetPriceIdByLookupKeyAsync($"{integrationKey}_{priceSuffix}");
            if (string.IsNullOrEmpty(priceId)) return false;

            // Add new item to subscription
            var subscriptionItems = subscription.Items.Data.Select(item => new SubscriptionItemOptions
            {
                Id = item.Id
            }).ToList();

            subscriptionItems.Add(new SubscriptionItemOptions { Price = priceId });

            await _stripeSubscriptionService.UpdateAsync(org.StripeSubscriptionId, new SubscriptionUpdateOptions
            {
                Items = subscriptionItems,
                ProrationBehavior = "create_prorations" // Charge immediately
            });

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding integration {Integration} for organization {OrganizationId}",
                integrationKey, organizationId);
            return false;
        }
    }

    public async Task<bool> RemoveIntegrationAsync(Guid organizationId, string integrationKey)
    {
        try
        {
            var org = await _context.Organizations.FindAsync(organizationId);
            if (org?.StripeSubscriptionId == null) return false;

            var subscription = await _stripeSubscriptionService.GetAsync(org.StripeSubscriptionId);
            if (subscription == null) return false;

            // Find the item to remove
            var itemToRemove = subscription.Items.Data.FirstOrDefault(item =>
                item.Price.LookupKey?.Contains(integrationKey) == true);

            if (itemToRemove == null) return false;

            var subscriptionItemService = new SubscriptionItemService();
            await subscriptionItemService.DeleteAsync(itemToRemove.Id, new SubscriptionItemDeleteOptions
            {
                ProrationBehavior = "none" // Takes effect at period end
            });

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing integration {Integration} for organization {OrganizationId}",
                integrationKey, organizationId);
            return false;
        }
    }

    public async Task<string> GetCustomerPortalUrlAsync(Guid organizationId)
    {
        try
        {
            var org = await _context.Organizations.FindAsync(organizationId);
            if (org?.StripeCustomerId == null)
            {
                throw new InvalidOperationException("Organization does not have a Stripe customer");
            }

            var appUrl = _configuration["App:BaseUrl"] ?? "http://localhost:4200";
            var options = new Stripe.BillingPortal.SessionCreateOptions
            {
                Customer = org.StripeCustomerId,
                ReturnUrl = $"{appUrl}/settings/subscription"
            };

            var portalService = new Stripe.BillingPortal.SessionService();
            var session = await portalService.CreateAsync(options);
            return session.Url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer portal URL for organization {OrganizationId}", organizationId);
            throw;
        }
    }

    public async Task<Subscription?> GetSubscriptionAsync(string subscriptionId)
    {
        try
        {
            return await _stripeSubscriptionService.GetAsync(subscriptionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving subscription {SubscriptionId}", subscriptionId);
            return null;
        }
    }

    public async Task<Price[]> GetPricesAsync()
    {
        try
        {
            var options = new PriceListOptions
            {
                Limit = 100,
                Active = true
            };
            var prices = await _priceService.ListAsync(options);
            return prices.Data.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving prices from Stripe");
            return Array.Empty<Price>();
        }
    }

    private async Task<string?> GetPriceIdByLookupKeyAsync(string lookupKey)
    {
        try
        {
            var options = new PriceListOptions
            {
                LookupKeys = new List<string> { lookupKey },
                Limit = 1
            };
            var prices = await _priceService.ListAsync(options);
            return prices.Data.FirstOrDefault()?.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding price by lookup key {LookupKey}", lookupKey);
            return null;
        }
    }
}