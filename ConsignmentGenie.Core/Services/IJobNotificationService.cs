namespace ConsignmentGenie.Core.Services;

public interface IJobNotificationService
{
    Task NotifySuccess(Guid shopId, string title, string message, string? actionUrl = null);
    Task NotifyWarning(Guid shopId, string title, string message, string? actionUrl = null);
    Task NotifyError(Guid shopId, string title, string message, string? actionUrl = null);
    Task NotifyInfo(Guid shopId, string title, string message, string? actionUrl = null);
}

public record JobNotification
{
    public string Type { get; init; } = "info";  // success, warning, error, info
    public string Title { get; init; } = "";
    public string Message { get; init; } = "";
    public string? ActionUrl { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}