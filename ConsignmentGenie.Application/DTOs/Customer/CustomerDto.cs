namespace ConsignmentGenie.Application.DTOs.Customer;

public class CustomerDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public Guid OrganizationId { get; set; }

    // Verification Status
    public bool HasGoogleAccount { get; set; }
    public bool HasAppleAccount { get; set; }
    public bool IsVerified { get; set; }

    // Account Activity
    public DateTime? FirstReservationAt { get; set; }
    public DateTime CreatedAt { get; set; }

    // Stats
    public int ActiveReservations { get; set; }
    public int CompletedReservations { get; set; }
}