using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Application.DTOs.Customer;

public class CustomerLoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string OrgSlug { get; set; } = string.Empty;

    public bool RememberMe { get; set; } = false;
}

public class CustomerLoginResponse
{
    public string Token { get; set; } = string.Empty;
    public CustomerDto Customer { get; set; } = null!;
    public Guid OrganizationId { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public class CustomerDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool IsEmailVerified { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string FullName => $"{FirstName} {LastName}";
}