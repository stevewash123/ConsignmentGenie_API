namespace ConsignmentGenie.Core.DTOs.Registration;

public class RegisterConsignorFromInvitationResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? ConsignorId { get; set; }
    public string? ConsignorNumber { get; set; }
}