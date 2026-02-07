using ConsignmentGenie.Core.DTOs;
using ConsignmentGenie.Core.DTOs.Notifications;
using ConsignmentGenie.Core.Interfaces;
using ConsignmentGenie.API.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Authorize]
public abstract class NotificationsControllerBase : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger _logger;

    protected abstract string Role { get; }

    protected NotificationsControllerBase(
        INotificationService notificationService,
        IHubContext<NotificationHub> hubContext,
        ILogger logger)
    {
        _notificationService = notificationService;
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated notifications for the authenticated user
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<NotificationDto>>> GetNotifications([FromQuery] NotificationQueryParams query)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var result = await _notificationService.GetNotificationsAsync(userId.Value, Role, query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting {Role} notifications for user {UserId}", Role, GetCurrentUserId());
            return StatusCode(500, "An error occurred while retrieving notifications");
        }
    }

    /// <summary>
    /// Get unread notification count for the authenticated user
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<ActionResult<object>> GetUnreadCount()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                _logger.LogWarning("GetUnreadCount called with no user ID in token for role {Role}", Role);
                return Unauthorized();
            }

            _logger.LogInformation("Getting unread count for user {UserId} with role {Role} via {ControllerRole} endpoint",
                userId.Value, Role, Role);

            var count = await _notificationService.GetUnreadCountAsync(userId.Value, Role);

            _logger.LogInformation("Returning unread count {Count} for user {UserId} with role {Role}",
                count, userId.Value, Role);

            return Ok(new { count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unread notification count for {Role} user {UserId}", Role, GetCurrentUserId());
            return StatusCode(500, "An error occurred while retrieving unread count");
        }
    }

    /// <summary>
    /// Mark a specific notification as read
    /// </summary>
    [HttpPost("{id}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            await _notificationService.MarkAsReadAsync(id, userId.Value);
            await BroadcastUnreadCountAsync(userId.Value, Role);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {NotificationId} as read for {Role} user {UserId}",
                id, Role, GetCurrentUserId());
            return StatusCode(500, "An error occurred while marking notification as read");
        }
    }

    /// <summary>
    /// Mark a specific notification as unread
    /// </summary>
    [HttpPost("{id}/unread")]
    public async Task<IActionResult> MarkAsUnread(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            await _notificationService.MarkAsUnreadAsync(id, userId.Value);
            await BroadcastUnreadCountAsync(userId.Value, Role);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {NotificationId} as unread for {Role} user {UserId}",
                id, Role, GetCurrentUserId());
            return StatusCode(500, "An error occurred while marking notification as unread");
        }
    }

    /// <summary>
    /// Mark all notifications as read for the authenticated user
    /// </summary>
    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            await _notificationService.MarkAllAsReadAsync(userId.Value, Role);
            await BroadcastUnreadCountAsync(userId.Value, Role);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read for {Role} user {UserId}", Role, GetCurrentUserId());
            return StatusCode(500, "An error occurred while marking notifications as read");
        }
    }

    /// <summary>
    /// Delete a specific notification (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            await _notificationService.DeleteAsync(id, userId.Value);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting notification {NotificationId} for {Role} user {UserId}",
                id, Role, GetCurrentUserId());
            return StatusCode(500, "An error occurred while deleting notification");
        }
    }

    /// <summary>
    /// Get notification preferences for the authenticated user
    /// </summary>
    [HttpGet("preferences")]
    public async Task<ActionResult<NotificationPreferencesDto>> GetPreferences()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var preferences = await _notificationService.GetPreferencesAsync(userId.Value, Role);
            return Ok(preferences);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification preferences for {Role} user {UserId}", Role, GetCurrentUserId());
            return StatusCode(500, "An error occurred while retrieving notification preferences");
        }
    }

    /// <summary>
    /// Mark a notification as important
    /// </summary>
    [HttpPost("{id}/important")]
    public async Task<IActionResult> MarkAsImportant(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            await _notificationService.MarkAsImportantAsync(id, userId.Value);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {NotificationId} as important for {Role} user {UserId}",
                id, Role, GetCurrentUserId());
            return StatusCode(500, "An error occurred while marking notification as important");
        }
    }

    /// <summary>
    /// Mark a notification as not important
    /// </summary>
    [HttpDelete("{id}/important")]
    public async Task<IActionResult> MarkAsNotImportant(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            await _notificationService.MarkAsNotImportantAsync(id, userId.Value);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {NotificationId} as not important for {Role} user {UserId}",
                id, Role, GetCurrentUserId());
            return StatusCode(500, "An error occurred while marking notification as not important");
        }
    }

    /// <summary>
    /// Update notification preferences for the authenticated user
    /// </summary>
    [HttpPut("preferences")]
    public async Task<IActionResult> UpdatePreferences([FromBody] UpdateNotificationPreferencesRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await _notificationService.UpdatePreferencesAsync(userId.Value, Role, request);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notification preferences for {Role} user {UserId}", Role, GetCurrentUserId());
            return StatusCode(500, "An error occurred while updating notification preferences");
        }
    }

    protected Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private async Task BroadcastUnreadCountAsync(Guid userId, string role)
    {
        try
        {
            var unreadCount = await _notificationService.GetUnreadCountAsync(userId, role);

            await _hubContext.Clients.User(userId.ToString())
                .SendAsync("UnreadCountUpdated", unreadCount);

            _logger.LogDebug("Broadcasted unread count {Count} for user {UserId} role {Role}",
                unreadCount, userId, role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast unread count for user {UserId} role {Role}", userId, role);
            // Don't throw - SignalR broadcast failure shouldn't break the main operation
        }
    }
}