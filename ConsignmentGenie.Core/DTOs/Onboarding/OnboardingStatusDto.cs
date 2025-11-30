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
}