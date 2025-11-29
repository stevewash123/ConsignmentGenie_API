using ConsignmentGenie.Core.DTOs;
using ConsignmentGenie.Core.DTOs.Notifications;
using ConsignmentGenie.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/owner")]
[Authorize(Roles = "Owner")]
public class OwnerController : ControllerBase
{
    private readonly IProviderNotificationService _notificationService;
    private readonly ILogger<OwnerController> _logger;

    public OwnerController(
        IProviderNotificationService notificationService,
        ILogger<OwnerController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated notifications for the authenticated owner
    /// </summary>
    [HttpGet("notifications")]
    public async Task<ActionResult<PagedResult<NotificationDto>>> GetNotifications([FromQuery] NotificationQueryParams queryParams)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var notifications = await _notificationService.GetNotificationsAsync(userId.Value, queryParams);
            return Ok(notifications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting owner notifications");
            return StatusCode(500, "An error occurred while retrieving notifications");
        }
    }

    /// <summary>
    /// Get unread notification count for the authenticated owner
    /// </summary>
    [HttpGet("notifications/unread-count")]
    public async Task<ActionResult<object>> GetUnreadCount()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var count = await _notificationService.GetUnreadCountAsync(userId.Value);
            return Ok(new { count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting owner unread notification count");
            return StatusCode(500, "An error occurred while retrieving unread count");
        }
    }

    /// <summary>
    /// Mark a specific notification as read
    /// </summary>
    [HttpPost("notifications/{id}/mark-read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            await _notificationService.MarkAsReadAsync(id, userId.Value);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking owner notification {NotificationId} as read", id);
            return StatusCode(500, "An error occurred while marking notification as read");
        }
    }

    /// <summary>
    /// Mark all notifications as read for the authenticated owner
    /// </summary>
    [HttpPost("notifications/mark-all-read")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            await _notificationService.MarkAllAsReadAsync(userId.Value);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all owner notifications as read");
            return StatusCode(500, "An error occurred while marking notifications as read");
        }
    }

    /// <summary>
    /// Delete a specific notification
    /// </summary>
    [HttpDelete("notifications/{id}")]
    public async Task<IActionResult> DeleteNotification(Guid id)
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
            _logger.LogError(ex, "Error deleting owner notification {NotificationId}", id);
            return StatusCode(500, "An error occurred while deleting notification");
        }
    }

    /// <summary>
    /// Get notification preferences for the authenticated owner
    /// </summary>
    [HttpGet("notifications/preferences")]
    public async Task<ActionResult<NotificationPreferencesDto>> GetPreferences()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var preferences = await _notificationService.GetPreferencesAsync(userId.Value);
            return Ok(preferences);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting owner notification preferences");
            return StatusCode(500, "An error occurred while retrieving notification preferences");
        }
    }

    /// <summary>
    /// Update notification preferences for the authenticated owner
    /// </summary>
    [HttpPut("notifications/preferences")]
    public async Task<IActionResult> UpdatePreferences([FromBody] UpdateNotificationPreferencesRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var preferences = await _notificationService.UpdatePreferencesAsync(userId.Value, request);
            return Ok(preferences);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating owner notification preferences");
            return StatusCode(500, "An error occurred while updating notification preferences");
        }
    }

    /// <summary>
    /// Delete a specific notification
    /// </summary>
    [HttpDelete("notifications/{id}")]
    public async Task<IActionResult> DeleteNotification(Guid id)
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
            _logger.LogError(ex, "Error deleting owner notification {NotificationId}", id);
            return StatusCode(500, "An error occurred while deleting notification");
        }
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}