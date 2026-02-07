using ConsignmentGenie.Core.Interfaces;
using ConsignmentGenie.API.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace ConsignmentGenie.API.Controllers;

[Route("api/admin/notifications")]
public class AdminNotificationsController : NotificationsControllerBase
{
    protected override string Role => "admin";

    public AdminNotificationsController(
        INotificationService notificationService,
        IHubContext<NotificationHub> hubContext,
        ILogger<AdminNotificationsController> logger)
        : base(notificationService, hubContext, logger)
    {
    }
}