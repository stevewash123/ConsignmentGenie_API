using ConsignmentGenie.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly ConsolidatedNotificationJob _notificationJob;
    private readonly ILogger<TestController> _logger;

    public TestController(ConsolidatedNotificationJob notificationJob, ILogger<TestController> logger)
    {
        _notificationJob = notificationJob;
        _logger = logger;
    }

    [HttpPost("trigger-notification/{notificationId}")]
    public async Task<IActionResult> TriggerNotification(Guid notificationId)
    {
        try
        {
            _logger.LogInformation("Manually triggering consolidated notification {NotificationId}", notificationId);
            await _notificationJob.ProcessConsolidatedNotificationAsync(notificationId);
            return Ok(new { success = true, message = "Notification processed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process notification {NotificationId}", notificationId);
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }
}