using ConsignmentGenie.Application.DTOs.Auth;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ConsignmentGenie.Application.Services;

public class UserRoleService : IUserRoleService
{
    private readonly ConsignmentGenieContext _context;
    private readonly ILogger<UserRoleService> _logger;

    public UserRoleService(ConsignmentGenieContext context, ILogger<UserRoleService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<UserContextDto?> GetUserContextAsync(Guid userId)
    {
        try
        {
            var user = await _context.Users
                .Include(u => u.RoleAssignments)
                .ThenInclude(ra => ra.Organization)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return null;

            return new UserContextDto
            {
                UserId = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Roles = user.RoleAssignments
                    .Where(ra => ra.IsActive)
                    .Select(ra => new UserRoleDto
                    {
                        Role = ra.Role,
                        OrganizationId = ra.OrganizationId,
                        OrganizationName = ra.Organization?.Name,
                        AssignedAt = ra.AssignedAt,
                        IsActive = ra.IsActive,
                        RoleData = ra.RoleData
                    })
                    .ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user context for user {UserId}", userId);
            return null;
        }
    }

    public async Task<bool> AddUserRoleAsync(AddUserRoleRequest request)
    {
        try
        {
            // Check if role assignment already exists
            var existingRole = await _context.UserRoleAssignments
                .FirstOrDefaultAsync(ra => ra.UserId == request.UserId &&
                                          ra.Role == request.Role &&
                                          ra.OrganizationId == request.OrganizationId);

            if (existingRole != null)
            {
                // Reactivate if it was deactivated
                if (!existingRole.IsActive)
                {
                    existingRole.IsActive = true;
                    existingRole.AssignedAt = DateTime.UtcNow;
                    existingRole.RoleData = request.RoleData;
                }
                else
                {
                    return false; // Role already exists and is active
                }
            }
            else
            {
                // Create new role assignment
                var roleAssignment = new UserRoleAssignment
                {
                    UserId = request.UserId,
                    Role = request.Role,
                    OrganizationId = request.OrganizationId,
                    RoleData = request.RoleData,
                    AssignedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.UserRoleAssignments.Add(roleAssignment);
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding role {Role} to user {UserId}", request.Role, request.UserId);
            return false;
        }
    }

    public async Task<bool> RemoveUserRoleAsync(Guid userId, UserRole role, Guid? organizationId = null)
    {
        try
        {
            var roleAssignment = await _context.UserRoleAssignments
                .FirstOrDefaultAsync(ra => ra.UserId == userId &&
                                          ra.Role == role &&
                                          ra.OrganizationId == organizationId &&
                                          ra.IsActive);

            if (roleAssignment == null)
                return false;

            roleAssignment.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing role {Role} from user {UserId}", role, userId);
            return false;
        }
    }

    public async Task<bool> HasRoleAsync(Guid userId, UserRole role, Guid? organizationId = null)
    {
        try
        {
            var query = _context.UserRoleAssignments
                .Where(ra => ra.UserId == userId && ra.Role == role && ra.IsActive);

            if (organizationId.HasValue)
                query = query.Where(ra => ra.OrganizationId == organizationId);

            return await query.AnyAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking role {Role} for user {UserId}", role, userId);
            return false;
        }
    }

    public async Task<List<UserContextDto>> GetUsersWithRoleAsync(UserRole role, Guid? organizationId = null)
    {
        try
        {
            var query = _context.UserRoleAssignments
                .Include(ra => ra.User)
                .Include(ra => ra.Organization)
                .Where(ra => ra.Role == role && ra.IsActive);

            if (organizationId.HasValue)
                query = query.Where(ra => ra.OrganizationId == organizationId);

            var roleAssignments = await query.ToListAsync();

            return roleAssignments
                .GroupBy(ra => ra.UserId)
                .Select(g => new UserContextDto
                {
                    UserId = g.Key,
                    Email = g.First().User.Email,
                    FullName = g.First().User.FullName,
                    Roles = g.Select(ra => new UserRoleDto
                    {
                        Role = ra.Role,
                        OrganizationId = ra.OrganizationId,
                        OrganizationName = ra.Organization?.Name,
                        AssignedAt = ra.AssignedAt,
                        IsActive = ra.IsActive,
                        RoleData = ra.RoleData
                    }).ToList()
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users with role {Role}", role);
            return new List<UserContextDto>();
        }
    }

    public async Task<bool> UpdateRoleDataAsync(Guid userId, UserRole role, Guid? organizationId, string roleData)
    {
        try
        {
            var roleAssignment = await _context.UserRoleAssignments
                .FirstOrDefaultAsync(ra => ra.UserId == userId &&
                                          ra.Role == role &&
                                          ra.OrganizationId == organizationId &&
                                          ra.IsActive);

            if (roleAssignment == null)
                return false;

            roleAssignment.RoleData = roleData;
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating role data for user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> SetRoleActiveAsync(Guid userId, UserRole role, Guid? organizationId, bool isActive)
    {
        try
        {
            var roleAssignment = await _context.UserRoleAssignments
                .FirstOrDefaultAsync(ra => ra.UserId == userId &&
                                          ra.Role == role &&
                                          ra.OrganizationId == organizationId);

            if (roleAssignment == null)
                return false;

            roleAssignment.IsActive = isActive;
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting role active status for user {UserId}", userId);
            return false;
        }
    }
}