using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ConsignmentGenie.API.Hubs;

[Authorize]
public class JobNotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        // Add user to their organization's group for targeted notifications
        var organizationId = Context.User?.FindFirst("organizationId")?.Value;
        if (!string.IsNullOrEmpty(organizationId))
        {
            var groupName = $"org_{organizationId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var organizationId = Context.User?.FindFirst("organizationId")?.Value;
        if (!string.IsNullOrEmpty(organizationId))
        {
            var groupName = $"org_{organizationId}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }
        await base.OnDisconnectedAsync(exception);
    }
}