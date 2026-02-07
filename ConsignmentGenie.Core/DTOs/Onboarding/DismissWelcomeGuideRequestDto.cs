using System.Text.Json.Serialization;

namespace ConsignmentGenie.Core.DTOs.Onboarding;

public class DismissWelcomeGuideRequestDto
{
    [JsonPropertyName("welcomeGuideCompleted")]
    public bool WelcomeGuideCompleted { get; set; }
}