using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Application.DTOs;

// Query Parameters
public class OwnerInvitationQueryParams
{
    public string? Search { get; set; }              // Search name, email
    public string? Status { get; set; }              // Pending, Accepted, Expired, Cancelled, or null for all
    public string? SortBy { get; set; } = "CreatedAt";    // CreatedAt, ExpiresAt, Name, Email
    public string? SortDirection { get; set; } = "desc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

// List View DTO
public class OwnerInvitationListDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string InvitedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsExpired { get; set; }
}

// Detail View DTO
public class OwnerInvitationDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string InvitedByName { get; set; } = string.Empty;
    public string InvitedByEmail { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsExpired { get; set; }
    public string InvitationUrl { get; set; } = string.Empty;
}

// Dashboard Metrics DTO
public class OwnerInvitationMetricsDto
{
    public int TotalInvitations { get; set; }
    public int PendingInvitations { get; set; }
    public int AcceptedInvitations { get; set; }
    public int ExpiredInvitations { get; set; }
    public int CancelledInvitations { get; set; }
    public int InvitationsThisMonth { get; set; }
    public decimal AcceptanceRate { get; set; }
}

// Create Request
public class CreateOwnerInvitationRequest
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(254)]
    public string Email { get; set; } = string.Empty;
}

// Registration Request (used by invited owner)
public class OwnerRegistrationRequest
{
    [Required, MaxLength(128)]
    public string Token { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string ShopName { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string Subdomain { get; set; } = string.Empty;
}

// Invitation validation response
public class ValidateInvitationResponse
{
    public bool IsValid { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string? ErrorMessage { get; set; }
}

// Registration completion response
public class OwnerRegistrationResponse
{
    public bool Success { get; set; }
    public Guid? UserId { get; set; }
    public Guid? OrganizationId { get; set; }
    public string? Token { get; set; }
    public string? RedirectUrl { get; set; }
    public string? ErrorMessage { get; set; }
}