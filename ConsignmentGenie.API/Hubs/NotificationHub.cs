using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace ConsignmentGenie.API.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();

        if (userId != null && userRole != null)
        {
            // Join user-specific group for targeted notifications
            await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");

            // Also join role-specific group if needed for broadcast messages
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Role_{userRole}");

            _logger.LogInformation("User {UserId} with role {Role} connected to NotificationHub", userId, userRole);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();

        if (userId != null && userRole != null)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Role_{userRole}");

            _logger.LogInformation("User {UserId} with role {Role} disconnected from NotificationHub", userId, userRole);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Client can call this to join specific notification groups
    /// </summary>
    public async Task JoinNotificationGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogDebug("Connection {ConnectionId} joined group {GroupName}", Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Client can call this to leave specific notification groups
    /// </summary>
    public async Task LeaveNotificationGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogDebug("Connection {ConnectionId} left group {GroupName}", Context.ConnectionId, groupName);
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private string? GetCurrentUserRole()
    {
        return Context.User?.FindFirst(ClaimTypes.Role)?.Value;
    }
}