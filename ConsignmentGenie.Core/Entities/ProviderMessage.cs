using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.Entities;

public class ProviderMessage : BaseEntity
{
    [Required]
    public Guid FromProviderId { get; set; }

    [Required]
    public Guid ToUserId { get; set; }

    [Required]
    public Guid OrganizationId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; } = false;

    public DateTime? ReadAt { get; set; }

    public string? Attachments { get; set; } // JSON array of file URLs

    public Guid? InReplyToMessageId { get; set; }

    // Navigation properties
    public Provider FromProvider { get; set; } = null!;
    public User ToUser { get; set; } = null!;
    public Organization Organization { get; set; } = null!;
    public ProviderMessage? InReplyToMessage { get; set; }
    public ICollection<ProviderMessage> Replies { get; set; } = new List<ProviderMessage>();
}