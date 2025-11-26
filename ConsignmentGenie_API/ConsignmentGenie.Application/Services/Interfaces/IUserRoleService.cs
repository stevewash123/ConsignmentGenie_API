using ConsignmentGenie.Application.DTOs.Auth;
using ConsignmentGenie.Core.Enums;

namespace ConsignmentGenie.Application.Services.Interfaces;

public interface IUserRoleService
{
    /// <summary>
    /// Get all role assignments for a user
    /// </summary>
    Task<UserContextDto?> GetUserContextAsync(Guid userId);

    /// <summary>
    /// Add a new role to a user
    /// </summary>
    Task<bool> AddUserRoleAsync(AddUserRoleRequest request);

    /// <summary>
    /// Remove a role from a user
    /// </summary>
    Task<bool> RemoveUserRoleAsync(Guid userId, UserRole role, Guid? organizationId = null);

    /// <summary>
    /// Check if a user has a specific role in an organization
    /// </summary>
    Task<bool> HasRoleAsync(Guid userId, UserRole role, Guid? organizationId = null);

    /// <summary>
    /// Get all users with a specific role in an organization
    /// </summary>
    Task<List<UserContextDto>> GetUsersWithRoleAsync(UserRole role, Guid? organizationId = null);

    /// <summary>
    /// Update role data (e.g., commission rates, permissions)
    /// </summary>
    Task<bool> UpdateRoleDataAsync(Guid userId, UserRole role, Guid? organizationId, string roleData);

    /// <summary>
    /// Activate/deactivate a role assignment
    /// </summary>
    Task<bool> SetRoleActiveAsync(Guid userId, UserRole role, Guid? organizationId, bool isActive);
}