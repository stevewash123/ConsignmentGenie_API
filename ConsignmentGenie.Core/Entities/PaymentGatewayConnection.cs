namespace ConsignmentGenie.Core.Entities;

public class PaymentGatewayConnection
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Provider { get; set; } = string.Empty; // "Stripe", "Square", "QuickBooks", etc.
    public string ConnectionName { get; set; } = string.Empty; // User-friendly name
    public string EncryptedConfig { get; set; } = string.Empty; // JSON config encrypted
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }

    // Navigation
    public Organization Organization { get; set; } = null!;
}