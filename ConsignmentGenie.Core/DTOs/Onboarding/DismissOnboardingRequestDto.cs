using System.Text.Json.Serialization;

namespace ConsignmentGenie.Core.DTOs.Onboarding;

public class DismissOnboardingRequestDto
{
    [JsonPropertyName("dismissed")]
    public bool Dismissed { get; set; }
}