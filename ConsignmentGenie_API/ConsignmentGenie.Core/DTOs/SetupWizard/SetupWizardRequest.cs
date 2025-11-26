namespace ConsignmentGenie.Core.DTOs.SetupWizard;

// Step 1: Shop Profile
public class UpdateShopProfileRequest
{
    public ShopProfileDto ShopProfile { get; set; } = new();
}

// Step 2: Business Settings
public class UpdateBusinessSettingsRequest
{
    public BusinessSettingsDto BusinessSettings { get; set; } = new();
}

// Step 3: Storefront Settings
public class UpdateStorefrontSettingsRequest
{
    public StorefrontSettingsDto StorefrontSettings { get; set; } = new();
}

// Step 4-7: Integration Setup
public class SetupIntegrationRequest
{
    public string IntegrationType { get; set; } = string.Empty;
    public Dictionary<string, string> Credentials { get; set; } = new();
}

// Step 8: Complete Setup
public class CompleteSetupRequest
{
    public bool StartTrial { get; set; } = true;
    public string? SubscriptionPlan { get; set; }
}

// Generic step update
public class UpdateSetupStepRequest
{
    public int StepNumber { get; set; }
    public object StepData { get; set; } = new();
}