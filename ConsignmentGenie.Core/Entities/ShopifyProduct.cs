using ConsignmentGenie.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.Entities;

public class ShopifyProduct : BaseEntity
{
    [Required]
    public Guid ItemId { get; set; }

    [Required]
    public Guid ShopifyConnectionId { get; set; }

    [Required]
    [MaxLength(50)]
    public string ShopifyProductId { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? ShopifyVariantId { get; set; }

    public DateTime LastSyncedAt { get; set; } = DateTime.UtcNow;

    public SyncStatus SyncStatus { get; set; } = SyncStatus.Synced;

    [MaxLength(500)]
    public string? LastSyncError { get; set; }

    public int SyncAttempts { get; set; } = 0;

    public DateTime? NextSyncAt { get; set; }

    // Navigation properties
    public Item Item { get; set; } = null!;
    public ShopifyConnection ShopifyConnection { get; set; } = null!;
}