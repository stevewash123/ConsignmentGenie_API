using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.DTOs.Registration;

public class RegisterClerkFromInvitationRequest
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    [Required]
    [MinLength(8)]
    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;
}