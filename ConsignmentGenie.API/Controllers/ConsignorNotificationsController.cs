using ConsignmentGenie.Core.DTOs;
using ConsignmentGenie.Core.DTOs.Notifications;
using ConsignmentGenie.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/consignor/notifications")]
[Authorize]
public class ConsignorNotificationsController : ControllerBase
{
    private readonly IConsignorNotificationService _notificationService;
    private readonly ILogger<ConsignorNotificationsController> _logger;

    public ConsignorNotificationsController(
        IConsignorNotificationService notificationService,
        ILogger<ConsignorNotificationsController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated notifications for the authenticated provider
    /// </summary>
    [HttpGet]
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
            _logger.LogError(ex, "Error getting notifications");
            return StatusCode(500, "An error occurred while retrieving notifications");
        }
    }

    /// <summary>
    /// Get unread notification count for the authenticated provider
    /// </summary>
    [HttpGet("unread-count")]
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
            _logger.LogError(ex, "Error getting unread notification count");
            return StatusCode(500, "An error occurred while retrieving unread count");
        }
    }

    /// <summary>
    /// Mark a specific notification as read
    /// </summary>
    [HttpPost("{id}/mark-read")]
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
            _logger.LogError(ex, "Error marking notification {NotificationId} as read", id);
            return StatusCode(500, "An error occurred while marking notification as read");
        }
    }

    /// <summary>
    /// Mark all notifications as read for the authenticated provider
    /// </summary>
    [HttpPost("mark-all-read")]
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
            _logger.LogError(ex, "Error marking all notifications as read");
            return StatusCode(500, "An error occurred while marking notifications as read");
        }
    }

    /// <summary>
    /// Delete a specific notification
    /// </summary>
    [HttpDelete("{id}")]
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
            _logger.LogError(ex, "Error deleting notification {NotificationId}", id);
            return StatusCode(500, "An error occurred while deleting notification");
        }
    }

    /// <summary>
    /// Get notification preferences for the authenticated provider
    /// </summary>
    [HttpGet("preferences")]
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
            _logger.LogError(ex, "Error getting notification preferences");
            return StatusCode(500, "An error occurred while retrieving notification preferences");
        }
    }

    /// <summary>
    /// Update notification preferences for the authenticated provider
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

            var preferences = await _notificationService.UpdatePreferencesAsync(userId.Value, request);
            return Ok(preferences);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notification preferences");
            return StatusCode(500, "An error occurred while updating notification preferences");
        }
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}