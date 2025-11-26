using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.DTOs.Items;

public class UpdateItemStatusRequest
{
    [Required]
    public string Status { get; set; } = string.Empty;  // Available, Removed (not Sold - that's via transaction)

    public string? Reason { get; set; }  // Why was status changed?
}