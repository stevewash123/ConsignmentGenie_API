using ConsignmentGenie.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Application.DTOs.Consignor;

public class CreateConsignorInvitationDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(254)]
    public string Email { get; set; } = string.Empty;
}

public class ConsignorInvitationDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public InvitationStatus Status { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string InvitedByEmail { get; set; } = string.Empty;
}

public class ConsignorInvitationResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public ConsignorInvitationDto? Invitation { get; set; }
}