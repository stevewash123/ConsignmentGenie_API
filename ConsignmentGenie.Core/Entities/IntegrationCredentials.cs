using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.Entities;

public class IntegrationCredentials : BaseEntity
{
    public Guid OrganizationId { get; set; }

    [Required]
    [MaxLength(50)]
    public string IntegrationType { get; set; } = string.Empty;  // 'quickbooks', 'stripe', 'sendgrid', 'cloudinary'

    [Required]
    public string CredentialsEncrypted { get; set; } = string.Empty;  // Encrypted JSON blob of credentials

    public DateTime? AccessTokenExpiresAt { get; set; }

    public DateTime? RefreshTokenExpiresAt { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? LastUsedAt { get; set; }

    public DateTime? LastErrorAt { get; set; }

    public string? LastErrorMessage { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
}