using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.DTOs.Items;

public class BulkStatusUpdateRequest
{
    [Required]
    public List<Guid> ItemIds { get; set; } = new();

    [Required]
    public string Status { get; set; } = string.Empty;

    public string? Reason { get; set; }
}