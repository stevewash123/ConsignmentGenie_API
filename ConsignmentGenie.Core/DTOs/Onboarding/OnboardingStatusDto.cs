using System.Text.Json.Serialization;

namespace ConsignmentGenie.Core.DTOs.Onboarding;

public class OnboardingStatusDto
{
    [JsonPropertyName("dismissed")]
    public bool Dismissed { get; set; }

    [JsonPropertyName("welcomeGuideCompleted")]
    public bool WelcomeGuideCompleted { get; set; }

    [JsonPropertyName("showModal")]
    public bool ShowModal { get; set; }

    [JsonPropertyName("setupChecklistDismissed")]
    public bool SetupChecklistDismissed { get; set; }

    [JsonPropertyName("steps")]
    public OnboardingStepsDto Steps { get; set; } = new OnboardingStepsDto();
}

public class OnboardingStepsDto
{
    [JsonPropertyName("hasProviders")]
    public bool HasProviders { get; set; }

    [JsonPropertyName("storefrontConfigured")]
    public bool StorefrontConfigured { get; set; }

    [JsonPropertyName("hasInventory")]
    public bool HasInventory { get; set; }

    [JsonPropertyName("quickBooksConnected")]
    public bool QuickBooksConnected { get; set; }

    [JsonPropertyName("agreementUploaded")]
    public bool AgreementUploaded { get; set; }

    [JsonPropertyName("squareConnected")]
    public bool SquareConnected { get; set; }

    [JsonPropertyName("payoutMethodConfigured")]
    public bool PayoutMethodConfigured { get; set; }
}