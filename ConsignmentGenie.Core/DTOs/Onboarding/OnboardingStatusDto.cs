namespace ConsignmentGenie.Core.DTOs.Onboarding;

public class OnboardingStatusDto
{
    public bool Dismissed { get; set; }
    public OnboardingStepsDto Steps { get; set; } = new OnboardingStepsDto();
}

public class OnboardingStepsDto
{
    public bool HasProviders { get; set; }
    public bool StorefrontConfigured { get; set; }
    public bool HasInventory { get; set; }
    public bool QuickBooksConnected { get; set; }
}