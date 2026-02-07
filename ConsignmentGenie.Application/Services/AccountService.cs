using ConsignmentGenie.Application.DTOs.Account;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ConsignmentGenie.Application.Services;

public class AccountService : IAccountService
{
    private readonly ConsignmentGenieContext _context;
    private readonly IStripeService _stripeService;
    private readonly ILogger<AccountService> _logger;

    public AccountService(
        ConsignmentGenieContext context,
        IStripeService stripeService,
        ILogger<AccountService> logger)
    {
        _context = context;
        _stripeService = stripeService;
        _logger = logger;
    }

    public async Task<AccountInfoDto> GetAccountInfoAsync(Guid organizationId)
    {
        _logger.LogInformation("Getting account information for organization {OrganizationId}", organizationId);

        var organization = await _context.Organizations
            .Include(o => o.Users.Where(u => u.Role == Core.Enums.UserRole.Owner))
            .Include(o => o.Consignors)
            .Include(o => o.Items)
            .Include(o => o.Transactions)
            .Include(o => o.Payouts)
            .FirstOrDefaultAsync(o => o.Id == organizationId);

        if (organization == null)
        {
            throw new InvalidOperationException($"Organization with ID {organizationId} not found");
        }

        var owner = organization.Users.FirstOrDefault(u => u.Role == Core.Enums.UserRole.Owner);

        // Get usage statistics
        var usageStats = await GetUsageStatsAsync(organizationId);

        // Get subscription plan details
        var subscriptionPlan = GetSubscriptionPlan(organization);

        // Create organization info
        var organizationInfo = new OrganizationInfoDto
        {
            Id = organization.Id,
            ShopName = organization.ShopName ?? organization.Name,
            OwnerEmail = owner?.Email ?? "Unknown",
            OwnerName = owner?.Email ?? "Unknown", // We don't store separate name field
            CreatedAt = organization.CreatedAt,
            StorefrontUrl = organization.StoreEnabled ? $"{organization.Slug}.consignmentgenie.com" : null,
            IsActive = organization.CachedSubscriptionStatus == "active" || organization.CachedSubscriptionStatus == "trialing"
        };

        return new AccountInfoDto
        {
            Organization = organizationInfo,
            CurrentPlan = subscriptionPlan,
            UsageStats = usageStats,
            BillingHistory = await GetBillingHistoryAsync(organization.StripeCustomerId),
            LastUpdated = DateTime.UtcNow
        };
    }

    private async Task<UsageStatsDto> GetUsageStatsAsync(Guid organizationId)
    {
        var totalConsignors = await _context.Consignors
            .Where(c => c.OrganizationId == organizationId)
            .CountAsync();

        var activeConsignors = await _context.Consignors
            .Where(c => c.OrganizationId == organizationId && c.Status == Core.Enums.ConsignorStatus.Active)
            .CountAsync();

        var totalItems = await _context.Items
            .Where(i => i.OrganizationId == organizationId)
            .CountAsync();

        var activeItems = await _context.Items
            .Where(i => i.OrganizationId == organizationId && i.Status == Core.Enums.ItemStatus.Available)
            .CountAsync();

        var totalSales = await _context.Transactions
            .Where(t => t.OrganizationId == organizationId && t.Status == "Completed")
            .CountAsync();

        var totalRevenue = await _context.Transactions
            .Where(t => t.OrganizationId == organizationId && t.Status == "Completed")
            .SumAsync(t => t.Total);

        var payoutsProcessed = await _context.Payouts
            .Where(p => p.OrganizationId == organizationId)
            .CountAsync();

        var totalPayouts = await _context.Payouts
            .Where(p => p.OrganizationId == organizationId)
            .SumAsync(p => p.Amount);

        return new UsageStatsDto
        {
            TotalConsignors = totalConsignors,
            ActiveConsignors = activeConsignors,
            TotalItems = totalItems,
            ActiveItems = activeItems,
            TotalSales = totalSales,
            TotalRevenue = totalRevenue,
            PayoutsProcessed = payoutsProcessed,
            TotalPayouts = totalPayouts,
            StatsDate = DateTime.UtcNow
        };
    }

    private SubscriptionPlanDto GetSubscriptionPlan(Core.Entities.Organization organization)
    {
        // Determine pricing based on founder tier
        var (basePrice, integrationPrice) = GetPricingForTier(organization);

        // Get active integrations
        var activeIntegrations = GetActiveIntegrations(organization);

        // Determine plan type from subscription status
        var planType = organization.IsFounderPricing ? "Founder" : "Standard";
        var isTrialActive = organization.CachedSubscriptionStatus == "trialing";

        // Calculate trial days remaining if applicable
        int? trialDaysRemaining = null;
        if (isTrialActive && organization.CachedPeriodEnd.HasValue)
        {
            trialDaysRemaining = Math.Max(0, (int)(organization.CachedPeriodEnd.Value - DateTime.UtcNow).TotalDays);
        }

        var plan = new SubscriptionPlanDto
        {
            PlanType = planType,
            BasePrice = basePrice,
            IntegrationPrice = integrationPrice,
            ActiveIntegrations = activeIntegrations,
            IsFounder = organization.IsFounderPricing,
            FounderTier = organization.FounderTier?.ToString(),
            FounderMessage = organization.IsFounderPricing ? "Locked forever while subscription active" : null,
            IsTrialActive = isTrialActive,
            TrialDaysRemaining = trialDaysRemaining,
            BillingPeriodStart = null, // Would need to get from Stripe
            BillingPeriodEnd = organization.CachedPeriodEnd,
            BillingInterval = "month", // Default for now
            NextBillingDate = organization.CachedPeriodEnd
        };

        return plan;
    }

    private (decimal basePrice, decimal integrationPrice) GetPricingForTier(Core.Entities.Organization organization)
    {
        if (organization.IsFounderPricing)
        {
            return organization.FounderTier switch
            {
                1 => (29m, 15m),
                2 => (39m, 15m),
                _ => (49m, 20m) // Regular pricing as fallback
            };
        }

        return (49m, 20m); // Regular pricing
    }

    private List<IntegrationDto> GetActiveIntegrations(Core.Entities.Organization organization)
    {
        var integrations = new List<IntegrationDto>();

        // Get integrations from Stripe subscription (ActiveIntegrations array)
        if (organization.ActiveIntegrations?.Length > 0)
        {
            foreach (var integrationKey in organization.ActiveIntegrations)
            {
                var integration = MapIntegrationKeyToDto(integrationKey, organization);
                if (integration != null)
                {
                    integrations.Add(integration);
                }
            }
        }

        return integrations;
    }

    private IntegrationDto? MapIntegrationKeyToDto(string integrationKey, Core.Entities.Organization organization)
    {
        return integrationKey switch
        {
            "quickbooks" => new IntegrationDto
            {
                Id = "quickbooks",
                Name = "QuickBooks Online",
                Type = "accounting",
                Description = "Sync transactions, items, and payouts with QuickBooks for seamless bookkeeping",
                IsActive = true,
                ConfigurationRequired = true,
                LastSyncDate = organization.QuickBooksLastSync,
                Status = organization.QuickBooksConnected ? "Connected" : "Available"
            },
            "stripe_payments" => new IntegrationDto
            {
                Id = "stripe_payments",
                Name = "Stripe Payments",
                Type = "payments",
                Description = "Accept credit cards and digital payments from customers",
                IsActive = true,
                ConfigurationRequired = false,
                Status = "Available"
            },
            "square_pos" => new IntegrationDto
            {
                Id = "square_pos",
                Name = "Square POS",
                Type = "pos",
                Description = "Sync items and sales from Square Point of Sale system",
                IsActive = true,
                ConfigurationRequired = true,
                Status = "Available"
            },
            _ => null
        };
    }

    private async Task<List<BillingHistoryDto>> GetBillingHistoryAsync(string? stripeCustomerId)
    {
        // For now, return empty list - in production, this would query Stripe API
        // TODO: Implement Stripe invoice history retrieval
        _logger.LogInformation("Getting billing history for Stripe customer {CustomerId}", stripeCustomerId);

        return new List<BillingHistoryDto>();
    }
}