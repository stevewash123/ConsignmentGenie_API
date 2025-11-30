namespace ConsignmentGenie.Core.DTOs.Registration;

public class RegisterProviderFromInvitationResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? ProviderId { get; set; }
    public string? ProviderNumber { get; set; }
}