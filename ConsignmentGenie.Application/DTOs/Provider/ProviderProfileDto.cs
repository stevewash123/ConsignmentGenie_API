namespace ConsignmentGenie.Application.DTOs.Provider;

public class ProviderProfileDto
{
    public Guid ProviderId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public decimal CommissionRate { get; set; }  // Read-only, set by owner
    public string? PreferredPaymentMethod { get; set; }
    public string? PaymentDetails { get; set; }
    public bool EmailNotifications { get; set; }
    public DateTime MemberSince { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
}

public class UpdateProviderProfileRequest
{
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? PreferredPaymentMethod { get; set; }
    public string? PaymentDetails { get; set; }
    public bool EmailNotifications { get; set; }
}