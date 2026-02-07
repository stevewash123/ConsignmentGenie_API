namespace ConsignmentGenie.Core.Services;

public interface IPayoutNotificationService
{
    Task SendReadyForPayoutNotificationsAsync(CancellationToken cancellationToken = default);
}