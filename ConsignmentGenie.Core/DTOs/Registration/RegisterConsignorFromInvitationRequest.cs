using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.DTOs.Registration;

public class RegisterConsignorFromInvitationRequest
{
    [Required]
    public string InvitationToken { get; set; } = string.Empty;

    [Required]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(6)]
    public string Password { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Address { get; set; }
}