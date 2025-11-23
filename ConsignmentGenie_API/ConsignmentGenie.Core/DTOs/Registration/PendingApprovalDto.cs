namespace ConsignmentGenie.Core.DTOs.Registration;

public class PendingApprovalDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? PreferredPaymentMethod { get; set; }
    public string? PaymentDetails { get; set; }
    public DateTime RequestedAt { get; set; }
}