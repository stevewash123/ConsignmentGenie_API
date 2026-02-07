using ConsignmentGenie.Core.Interfaces;
using ConsignmentGenie.API.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace ConsignmentGenie.API.Controllers;

[Route("api/consignor/notifications")]
public class ConsignorNotificationsController : NotificationsControllerBase
{
    protected override string Role => "consignor";

    public ConsignorNotificationsController(
        INotificationService notificationService,
        IHubContext<NotificationHub> hubContext,
        ILogger<ConsignorNotificationsController> logger)
        : base(notificationService, hubContext, logger)
    {
    }
}