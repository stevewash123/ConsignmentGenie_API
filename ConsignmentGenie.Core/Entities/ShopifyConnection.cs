using ConsignmentGenie.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.Entities;

public class ShopifyConnection : BaseEntity
{
    [Required]
    public Guid OrganizationId { get; set; }

    [Required]
    [MaxLength(100)]
    public string ShopifyShopDomain { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string AccessToken { get; set; } = string.Empty; // Store encrypted

    [MaxLength(100)]
    public string? ShopifyShopId { get; set; }

    public DateTime? LastSyncAt { get; set; }

    public bool AutoSync { get; set; } = true;

    public SyncDirection SyncDirection { get; set; } = SyncDirection.Bidirectional;

    public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    public string? SyncSettings { get; set; } // JSON: sync preferences, mappings

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public ICollection<ShopifyProduct> ShopifyProducts { get; set; } = new List<ShopifyProduct>();
    public ICollection<ShopifySyncLog> SyncLogs { get; set; } = new List<ShopifySyncLog>();
}