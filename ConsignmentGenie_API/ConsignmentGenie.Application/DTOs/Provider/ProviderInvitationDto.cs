using ConsignmentGenie.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Application.DTOs.Provider;

public class CreateProviderInvitationDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(254)]
    public string Email { get; set; } = string.Empty;
}

public class ProviderInvitationDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public InvitationStatus Status { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string InvitedByEmail { get; set; } = string.Empty;
}

public class ProviderInvitationResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public ProviderInvitationDto? Invitation { get; set; }
}