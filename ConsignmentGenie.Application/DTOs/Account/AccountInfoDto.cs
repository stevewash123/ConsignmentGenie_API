using ConsignmentGenie.Core.Enums;

namespace ConsignmentGenie.Application.DTOs.Account;

public class AccountInfoDto
{
    public OrganizationInfoDto Organization { get; set; } = new();
    public SubscriptionPlanDto CurrentPlan { get; set; } = new();
    public UsageStatsDto UsageStats { get; set; } = new();
    public List<BillingHistoryDto> BillingHistory { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}

public class OrganizationInfoDto
{
    public Guid Id { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public string OwnerEmail { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? StorefrontUrl { get; set; }
    public bool IsActive { get; set; }
}

public class SubscriptionPlanDto
{
    public string PlanType { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public decimal IntegrationPrice { get; set; }
    public List<IntegrationDto> ActiveIntegrations { get; set; } = new();
    public bool IsFounder { get; set; }
    public string? FounderTier { get; set; }
    public string? FounderMessage { get; set; }
    public bool IsTrialActive { get; set; }
    public int? TrialDaysRemaining { get; set; }
    public DateTime? BillingPeriodStart { get; set; }
    public DateTime? BillingPeriodEnd { get; set; }
    public string BillingInterval { get; set; } = "month";
    public DateTime? NextBillingDate { get; set; }
}

public class IntegrationDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool ConfigurationRequired { get; set; }
    public DateTime? LastSyncDate { get; set; }
    public string? Status { get; set; }
}

public class UsageStatsDto
{
    public int TotalConsignors { get; set; }
    public int ActiveConsignors { get; set; }
    public int TotalItems { get; set; }
    public int ActiveItems { get; set; }
    public int TotalSales { get; set; }
    public decimal TotalRevenue { get; set; }
    public int PayoutsProcessed { get; set; }
    public decimal TotalPayouts { get; set; }
    public DateTime StatsDate { get; set; }
}

public class BillingHistoryDto
{
    public string InvoiceId { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? InvoiceUrl { get; set; }
    public string? PaymentMethod { get; set; }
}

public class IntegrationsInfoDto
{
    public List<IntegrationDto> ActiveIntegrations { get; set; } = new();
    public List<IntegrationDto> AvailableIntegrations { get; set; } = new();
    public int MaxIntegrations { get; set; }
    public decimal IntegrationPrice { get; set; }
}