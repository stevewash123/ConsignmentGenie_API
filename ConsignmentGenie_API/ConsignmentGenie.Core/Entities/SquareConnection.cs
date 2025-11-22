using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.Entities;

public class SquareConnection : BaseEntity
{
    public Guid OrganizationId { get; set; }

    public bool IsConnected { get; set; }

    [MaxLength(100)]
    public string? MerchantId { get; set; }

    public string? AccessToken { get; set; }  // Encrypted

    public string? RefreshToken { get; set; }  // Encrypted

    public DateTime? TokenExpiry { get; set; }

    [MaxLength(100)]
    public string? LocationId { get; set; }

    [MaxLength(200)]
    public string? LocationName { get; set; }

    public DateTime? LastSyncAt { get; set; }

    public bool AutoSync { get; set; } = true;

    [MaxLength(50)]
    public string SyncSchedule { get; set; } = "0 0 * * *";  // Cron expression - daily at midnight

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public ICollection<SquareSyncLog> SyncLogs { get; set; } = new List<SquareSyncLog>();
}