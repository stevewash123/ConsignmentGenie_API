using ConsignmentGenie.Core.Enums;

namespace ConsignmentGenie.Core.DTOs.SetupWizard;

public class TrialSubscriptionDto
{
    public string Status { get; set; } = "pending";
    public DateTime? TrialStartedAt { get; set; }
    public DateTime? TrialEndsAt { get; set; }
    public int DaysRemaining { get; set; }
    public int TrialExtensionsUsed { get; set; }
    public bool CanExtendTrial { get; set; }

    // Subscription Info
    public SubscriptionTier SubscriptionTier { get; set; } = SubscriptionTier.Basic;
    public string? SubscriptionPlan { get; set; }
    public string? StripeSubscriptionStatus { get; set; }
    public DateTime? SubscriptionStartedAt { get; set; }
    public DateTime? CurrentPeriodEnd { get; set; }
}

public class SetupCompleteDto
{
    public string OrganizationName { get; set; } = string.Empty;
    public string ShopName { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public bool StoreEnabled { get; set; }
    public DateTime CompletedAt { get; set; }
    public TrialSubscriptionDto TrialInfo { get; set; } = new();
    public IntegrationStatusDto IntegrationsStatus { get; set; } = new();
}