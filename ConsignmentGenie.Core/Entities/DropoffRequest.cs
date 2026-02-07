using System.Text.Json;
using ConsignmentGenie.Core.DTOs;

namespace ConsignmentGenie.Core.Entities;

public class DropoffRequest : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid ConsignorId { get; set; }

    public DateOnly? PlannedDate { get; set; }
    public string? PlannedTimeSlot { get; set; }
    public string? ConsignorMessage { get; set; }
    public string? OwnerNotes { get; set; }

    public string ItemsJson { get; set; } = "[]";
    public int ItemCount { get; set; }
    public decimal SuggestedTotal { get; set; }

    public string Status { get; set; } = DropoffRequestStatus.Pending;

    public DateTime? ReceivedAt { get; set; }
    public DateTime? ImportedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelReason { get; set; }

    // Rejection support
    public DateTime? RejectedAt { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime? ReopenedAt { get; set; }
    public DateTime? PhotosPurgedAt { get; set; }

    // Denormalized aggregate for minimum price total
    public decimal? MinimumTotal { get; set; }

    // Navigation
    public Organization Organization { get; set; } = null!;
    public Consignor Consignor { get; set; } = null!;

    // Helper methods
    public List<DropoffItemDto> GetItems()
        => JsonSerializer.Deserialize<List<DropoffItemDto>>(ItemsJson) ?? new();

    public void SetItems(List<DropoffItemDto> items)
    {
        ItemsJson = JsonSerializer.Serialize(items);
        ItemCount = items.Count;
        SuggestedTotal = items.Sum(i => i.SuggestedPrice);
        MinimumTotal = items.Where(i => i.MinimumPrice.HasValue).Sum(i => i.MinimumPrice!.Value);
    }
}

public static class DropoffRequestStatus
{
    public const string Pending = "pending";
    public const string Received = "received";
    public const string Imported = "imported";
    public const string Cancelled = "cancelled";
    public const string Rejected = "rejected";
}

public static class DropoffTimeSlot
{
    public const string Morning = "Morning";
    public const string Afternoon = "Afternoon";
    public const string Evening = "Evening";
    public const string Anytime = "Anytime";
}