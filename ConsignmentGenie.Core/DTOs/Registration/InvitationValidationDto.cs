namespace ConsignmentGenie.Core.DTOs.Registration;

public class InvitationValidationDto
{
    public bool IsValid { get; set; }
    public string? Message { get; set; }
    public string? ShopName { get; set; }
    public string? InvitedName { get; set; }
    public string? InvitedEmail { get; set; }
    public DateTime? ExpirationDate { get; set; }
}