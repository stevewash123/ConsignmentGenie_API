using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Stripe;

namespace ConsignmentGenie.Application.Services;

public interface IFeatureGatingService
{
    Task<bool> HasAccessAsync(Guid organizationId, string feature);
    Task<string?> GetUpgradePromptAsync(Guid organizationId, string feature);
    Task<bool> IsTrialActiveAsync(Guid organizationId);
    Task<SubscriptionInfo?> GetSubscriptionInfoAsync(Guid organizationId);
}

public class SubscriptionInfo
{
    public string Status { get; set; } = string.Empty;
    public bool IsTrialing { get; set; }
    public bool IsActive { get; set; }
    public bool IsFounder { get; set; }
    public int? FounderTier { get; set; }
    public DateTime? TrialEnd { get; set; }
    public DateTime? CurrentPeriodEnd { get; set; }
    public List<string> ActiveIntegrations { get; set; } = new();
    public decimal MonthlyTotal { get; set; }
}

public class FeatureGatingService : IFeatureGatingService
{
    private readonly ConsignmentGenieContext _context;
    private readonly ILogger<FeatureGatingService> _logger;
    private readonly ISubscriptionService _subscriptionService;

    private readonly string[] _baseFeatures = {
        "inventory",
        "consignors",
        "manual_sales",
        "reports",
        "notifications",
        "storefront"
    };

    public FeatureGatingService(
        ConsignmentGenieContext context,
        ILogger<FeatureGatingService> logger,
        ISubscriptionService subscriptionService)
    {
        _context = context;
        _logger = logger;
        _subscriptionService = subscriptionService;
    }

    public async Task<bool> HasAccessAsync(Guid organizationId, string feature)
    {
        try
        {
            var org = await _context.Organizations.FindAsync(organizationId);
            if (org == null)
            {
                _logger.LogWarning("Organization {OrganizationId} not found for feature check", organizationId);
                return false;
            }

            // Get current status from Stripe (or use cached for performance)
            var subscription = await GetOrRefreshSubscriptionAsync(org);

            if (subscription == null)
            {
                _logger.LogWarning("No subscription found for organization {OrganizationId}", organizationId);
                return false;
            }

            // Must be trialing or active
            if (subscription.Status != "trialing" && subscription.Status != "active")
            {
                return false;
            }

            // Trial gets full access to everything
            if (subscription.Status == "trialing")
            {
                return true;
            }

            // Base features always available for active subscribers
            if (IsBaseFeature(feature))
            {
                return true;
            }

            // Check if integration is in subscription line items
            return subscription.Items.Data
                .Select(item => item.Price.LookupKey)
                .Where(key => key != null && !key.StartsWith("base_"))
                .Select(key => ExtractFeatureName(key))
                .Contains(feature);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking feature access for organization {OrganizationId} and feature {Feature}",
                organizationId, feature);
            return false;
        }
    }

    public async Task<string?> GetUpgradePromptAsync(Guid organizationId, string feature)
    {
        try
        {
            var org = await _context.Organizations.FindAsync(organizationId);
            if (org == null) return null;

            var price = org.IsFounderPricing ? "$15" : "$20";

            return feature switch
            {
                "square_pos" => $"Add Square POS for +{price}/mo",
                "square_online" => $"Add Square Online for +{price}/mo",
                "quickbooks" => $"Add QuickBooks for +{price}/mo",
                "dwolla" => $"Add automated ACH payouts for +{price}/mo",
                _ => null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting upgrade prompt for organization {OrganizationId} and feature {Feature}",
                organizationId, feature);
            return null;
        }
    }

    public async Task<bool> IsTrialActiveAsync(Guid organizationId)
    {
        try
        {
            var org = await _context.Organizations.FindAsync(organizationId);
            if (org?.StripeSubscriptionId == null) return false;

            var subscription = await _subscriptionService.GetSubscriptionAsync(org.StripeSubscriptionId);
            return subscription?.Status == "trialing";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking trial status for organization {OrganizationId}", organizationId);
            return false;
        }
    }

    public async Task<SubscriptionInfo?> GetSubscriptionInfoAsync(Guid organizationId)
    {
        try
        {
            var org = await _context.Organizations.FindAsync(organizationId);
            if (org?.StripeSubscriptionId == null) return null;

            var subscription = await _subscriptionService.GetSubscriptionAsync(org.StripeSubscriptionId);
            if (subscription == null) return null;

            var activeIntegrations = subscription.Items.Data
                .Select(item => item.Price.LookupKey)
                .Where(key => key != null && !key.StartsWith("base_"))
                .Select(key => ExtractFeatureName(key))
                .ToList();

            var monthlyTotal = subscription.Items.Data.Sum(item => item.Price.UnitAmount ?? 0) / 100m;

            return new SubscriptionInfo
            {
                Status = subscription.Status,
                IsTrialing = subscription.Status == "trialing",
                IsActive = subscription.Status == "active",
                IsFounder = org.IsFounderPricing,
                FounderTier = org.FounderTier,
                TrialEnd = subscription.TrialEnd,
                CurrentPeriodEnd = subscription.CurrentPeriodEnd is DateTime dt ? dt : DateTime.UtcNow.AddDays(30),
                ActiveIntegrations = activeIntegrations,
                MonthlyTotal = monthlyTotal
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription info for organization {OrganizationId}", organizationId);
            return null;
        }
    }

    private bool IsBaseFeature(string feature)
    {
        return _baseFeatures.Contains(feature);
    }

    private string ExtractFeatureName(string? lookupKey)
    {
        if (string.IsNullOrEmpty(lookupKey)) return string.Empty;

        // Convert "square_pos_regular" or "square_pos_founder" to "square_pos"
        var parts = lookupKey.Split('_');
        if (parts.Length >= 2)
        {
            return $"{parts[0]}_{parts[1]}";
        }
        return lookupKey;
    }

    private async Task<Subscription?> GetOrRefreshSubscriptionAsync(Organization org)
    {
        if (string.IsNullOrEmpty(org.StripeSubscriptionId))
        {
            return null;
        }

        try
        {
            // Get current subscription from Stripe
            var subscription = await _subscriptionService.GetSubscriptionAsync(org.StripeSubscriptionId);

            if (subscription != null)
            {
                // Update cached status for quick access
                org.CachedSubscriptionStatus = subscription.Status;
                org.CachedPeriodEnd = subscription.CurrentPeriodEnd is DateTime dt2 ? dt2 : DateTime.UtcNow.AddDays(30);
                org.ActiveIntegrations = subscription.Items.Data
                    .Select(item => item.Price.LookupKey)
                    .Where(key => key != null && !key.StartsWith("base_"))
                    .Select(key => ExtractFeatureName(key))
                    .ToArray();

                await _context.SaveChangesAsync();
            }

            return subscription;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing subscription for organization {OrganizationId}", org.Id);
            return null;
        }
    }
}