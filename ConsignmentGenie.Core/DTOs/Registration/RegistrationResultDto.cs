using ConsignmentGenie.Core.Enums;

namespace ConsignmentGenie.Core.DTOs.Registration;

public class RegistrationResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string>? Errors { get; set; }

    // JWT token fields for successful registration
    public string? Token { get; set; }
    public Guid? UserId { get; set; }
    public string? Email { get; set; }
    public UserRole? Role { get; set; }
    public Guid? OrganizationId { get; set; }
    public string? OrganizationName { get; set; }
    public DateTime? ExpiresAt { get; set; }
}