using ConsignmentGenie.Core.Enums;

namespace ConsignmentGenie.Application.DTOs.Auth;

public class UserRoleDto
{
    public UserRole Role { get; set; }
    public Guid? OrganizationId { get; set; }
    public string? OrganizationName { get; set; }
    public DateTime AssignedAt { get; set; }
    public bool IsActive { get; set; }
    public string? RoleData { get; set; }
}

public class AddUserRoleRequest
{
    public Guid UserId { get; set; }
    public UserRole Role { get; set; }
    public Guid? OrganizationId { get; set; }
    public string? RoleData { get; set; }
}

public class UserContextDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public List<UserRoleDto> Roles { get; set; } = new();

    /// <summary>
    /// Get the primary role for backward compatibility
    /// </summary>
    public UserRole PrimaryRole => Roles.FirstOrDefault()?.Role ?? UserRole.Customer;

    /// <summary>
    /// Check if user has a specific role in any organization
    /// </summary>
    public bool HasRole(UserRole role) => Roles.Any(r => r.Role == role && r.IsActive);

    /// <summary>
    /// Check if user has a specific role in a specific organization
    /// </summary>
    public bool HasRoleInOrganization(UserRole role, Guid organizationId) =>
        Roles.Any(r => r.Role == role && r.OrganizationId == organizationId && r.IsActive);

    /// <summary>
    /// Get all organizations where user has owner role
    /// </summary>
    public List<Guid> GetOwnedOrganizations() =>
        Roles.Where(r => r.Role == UserRole.Owner && r.IsActive && r.OrganizationId.HasValue)
             .Select(r => r.OrganizationId!.Value)
             .ToList();

    /// <summary>
    /// Get all organizations where user is a provider
    /// </summary>
    public List<Guid> GetProviderOrganizations() =>
        Roles.Where(r => r.Role == UserRole.Consignor && r.IsActive && r.OrganizationId.HasValue)
             .Select(r => r.OrganizationId!.Value)
             .ToList();
}