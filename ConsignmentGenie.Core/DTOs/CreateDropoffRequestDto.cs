using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.DTOs;

public class CreateDropoffRequestDto
{
    [Required]
    [MinLength(1, ErrorMessage = "At least one item is required")]
    public List<DropoffItemDto> Items { get; set; } = new();

    public DateOnly? PlannedDate { get; set; }
    public string? PlannedTimeSlot { get; set; }

    [MaxLength(500)]
    public string? Message { get; set; }
}

public class DropoffRequestListDto
{
    public Guid Id { get; set; }
    public int ItemCount { get; set; }
    public decimal SuggestedTotal { get; set; }
    public DateOnly? PlannedDate { get; set; }
    public string? PlannedTimeSlot { get; set; }
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? ImportedAt { get; set; }
}

public class DropoffRequestDetailDto : DropoffRequestListDto
{
    public List<DropoffItemDto> Items { get; set; } = new();
    public string? Message { get; set; }
    public ShopSummaryDto Shop { get; set; } = null!;
}

public class ShopSummaryDto
{
    public string Name { get; set; } = "";
    public string? Address { get; set; }
    public string? Phone { get; set; }
}

// Owner-specific DTOs
public class OwnerDropoffRequestListDto : DropoffRequestListDto
{
    public ConsignorSummaryDto Consignor { get; set; } = null!;
}

public class OwnerDropoffRequestDetailDto : DropoffRequestDetailDto
{
    public ConsignorSummaryDto Consignor { get; set; } = null!;
    public DateTime? ReceivedAt { get; set; }
    public string? OwnerNotes { get; set; }
    public decimal? MinimumTotal { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime? ReopenedAt { get; set; }
    public DateTime? PhotosPurgedAt { get; set; }
}

public class ConsignorSummaryDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? Email { get; set; }
    public string? Phone { get; set; }
}

public class UpdateDropoffStatusDto
{
    [Required]
    public string Status { get; set; } = "";
    public string? OwnerNotes { get; set; }
}

public class ImportDropoffRequestDto
{
    public string? OwnerNotes { get; set; }
}

public class RejectDropoffRequestDto
{
    [Required]
    [MaxLength(500)]
    public string RejectionReason { get; set; } = "";
}

public class ReopenDropoffRequestDto
{
    [MaxLength(500)]
    public string? OwnerNotes { get; set; }
}