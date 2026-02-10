namespace ConsignmentGenie.Core.DTOs.Registration;

public class RegisterClerkFromInvitationResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Token { get; set; }
    public UserDto? User { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
}