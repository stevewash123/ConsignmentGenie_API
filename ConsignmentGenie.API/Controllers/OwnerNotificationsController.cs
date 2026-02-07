using ConsignmentGenie.Core.Interfaces;
using ConsignmentGenie.API.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace ConsignmentGenie.API.Controllers;

[Route("api/owner/notifications")]
public class OwnerNotificationsController : NotificationsControllerBase
{
    protected override string Role => "owner";

    public OwnerNotificationsController(
        INotificationService notificationService,
        IHubContext<NotificationHub> hubContext,
        ILogger<OwnerNotificationsController> logger)
        : base(notificationService, hubContext, logger)
    {
    }
}