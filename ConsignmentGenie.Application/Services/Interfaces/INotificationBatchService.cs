using ConsignmentGenie.Core.Enums;

namespace ConsignmentGenie.Application.Services.Interfaces;

/// <summary>
/// Service for batch processing consolidated notification reports
/// Handles user preference-based batching for daily/weekly reports
/// </summary>
public interface INotificationBatchService
{
    /// <summary>
    /// Processes daily batch notifications (sales summary, payout ready)
    /// Called by background job service daily
    /// </summary>
    Task ProcessDailyBatchAsync(DateTime? forDate = null);

    /// <summary>
    /// Processes weekly batch notifications (weekly reports, payout ready)
    /// Called by background job service on configured days
    /// </summary>
    Task ProcessWeeklyBatchAsync(DayOfWeek? forDay = null, DateTime? forDate = null);

    /// <summary>
    /// Processes monthly batch notifications (statements, reports)
    /// Called by background job service monthly
    /// </summary>
    Task ProcessMonthlyBatchAsync(DateTime? forDate = null);

    /// <summary>
    /// Gets users who should receive batch notifications of a specific type
    /// Groups by delivery preferences and timing
    /// </summary>
    Task<BatchDeliveryGroups> GetBatchDeliveryGroupsAsync(string notificationType, BatchFrequency frequency);

    /// <summary>
    /// Creates a consolidated report for a user containing multiple notifications
    /// </summary>
    Task<ConsolidatedReport> CreateConsolidatedReportAsync(Guid userId, string role, string reportType, DateTime periodStart, DateTime periodEnd);

    /// <summary>
    /// Sends a consolidated report via user's preferred channels
    /// </summary>
    Task SendConsolidatedReportAsync(ConsolidatedReport report);

    /// <summary>
    /// Validates that batch processing can run (prevents duplicate sends)
    /// </summary>
    Task<bool> CanProcessBatchAsync(string batchType, DateTime forDate);

    /// <summary>
    /// Records batch processing completion to prevent duplicates
    /// </summary>
    Task RecordBatchProcessingAsync(string batchType, DateTime forDate, BatchProcessingResult result);
}

/// <summary>
/// Groups of users by their batch delivery preferences
/// </summary>
public class BatchDeliveryGroups
{
    public Dictionary<string, List<BatchDeliveryTarget>> Groups { get; set; } = new();
    public DateTime ProcessingDate { get; set; }
    public string NotificationType { get; set; } = string.Empty;
}

/// <summary>
/// Target for batch notification delivery
/// </summary>
public class BatchDeliveryTarget
{
    public Guid UserId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public Dictionary<NotificationChannel, bool> ChannelPreferences { get; set; } = new();
    public string PreferenceGroup { get; set; } = string.Empty; // e.g., "monday", "daily_9am"
}

/// <summary>
/// Consolidated report containing multiple related notifications/data
/// </summary>
public class ConsolidatedReport
{
    public Guid UserId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string ReportType { get; set; } = string.Empty; // "daily_sales", "weekly_payout", etc.
    public string Title { get; set; } = string.Empty;
    public string HtmlContent { get; set; } = string.Empty;
    public string TextContent { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public DateTime GeneratedAt { get; set; }

    public List<ReportSection> Sections { get; set; } = new();
    public Dictionary<string, object> Data { get; set; } = new(); // Raw data for report generation
    public List<ReportAttachment> Attachments { get; set; } = new();
}

/// <summary>
/// Section within a consolidated report
/// </summary>
public class ReportSection
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int Order { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
}

/// <summary>
/// Attachment for consolidated reports (CSV exports, etc.)
/// </summary>
public class ReportAttachment
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Frequency for batch processing
/// </summary>
public enum BatchFrequency
{
    Daily,
    Weekly,
    Monthly
}

/// <summary>
/// Result of batch processing operation
/// </summary>
public class BatchProcessingResult
{
    public string BatchType { get; set; } = string.Empty;
    public DateTime ProcessedDate { get; set; }
    public int TotalTargets { get; set; }
    public int SuccessfulSends { get; set; }
    public int FailedSends { get; set; }
    public List<string> Errors { get; set; } = new();
    public TimeSpan ProcessingTime { get; set; }
}