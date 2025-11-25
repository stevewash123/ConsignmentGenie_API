using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Application.DTOs.Customer;

public class CustomerRegistrationRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string LastName { get; set; } = string.Empty;

    [Phone]
    public string? PhoneNumber { get; set; }

    [Required]
    public string OrgSlug { get; set; } = string.Empty;
}

public class CustomerRegistrationResponse
{
    public Guid CustomerId { get; set; }
    public Guid OrganizationId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool RequiresEmailVerification { get; set; } = true;
}