using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.DTOs.Notifications;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Core.Utilities;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace ConsignmentGenie.Application.Services;

/// <summary>
/// Batch processing service for consolidated notification reports
/// Handles user preference-based batching and report generation
/// </summary>
public class NotificationBatchService : INotificationBatchService
{
    private readonly ConsignmentGenieContext _context;
    private readonly INotificationPreferenceService _preferenceService;
    private readonly IEmailComplianceService _emailCompliance;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<NotificationBatchService> _logger;

    // TODO: Inject actual email service when available
    // private readonly IEmailService _emailService;

    public NotificationBatchService(
        ConsignmentGenieContext context,
        INotificationPreferenceService preferenceService,
        IEmailComplianceService emailCompliance,
        IHttpContextAccessor httpContextAccessor,
        ILogger<NotificationBatchService> logger)
    {
        _context = context;
        _preferenceService = preferenceService;
        _emailCompliance = emailCompliance;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task ProcessDailyBatchAsync(DateTime? forDate = null)
    {
        var processDate = forDate ?? DateTime.UtcNow.Date;
        var batchType = "daily_batch";

        if (!await CanProcessBatchAsync(batchType, processDate))
        {
            _logger.LogInformation("Daily batch for {Date} already processed, skipping", processDate);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var result = new BatchProcessingResult
        {
            BatchType = batchType,
            ProcessedDate = processDate
        };

        try
        {
            _logger.LogInformation("Starting daily batch processing for {Date}", processDate);

            // Process daily sales summary
            await ProcessDailySalesSummaryAsync(processDate, result);

            // Process daily payout ready notifications
            await ProcessDailyPayoutReadyAsync(processDate, result);

            stopwatch.Stop();
            result.ProcessingTime = stopwatch.Elapsed;

            await RecordBatchProcessingAsync(batchType, processDate, result);

            _logger.LogInformation("Completed daily batch processing for {Date}: {Success}/{Total} successful, took {Duration}ms",
                processDate, result.SuccessfulSends, result.TotalTargets, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process daily batch for {Date}", processDate);
            result.Errors.Add($"Fatal error: {ex.Message}");
            await RecordBatchProcessingAsync(batchType, processDate, result);
            throw;
        }
    }

    public async Task ProcessWeeklyBatchAsync(DayOfWeek? forDay = null, DateTime? forDate = null)
    {
        var processDate = forDate ?? DateTime.UtcNow.Date;
        var dayOfWeek = forDay ?? processDate.DayOfWeek;
        var batchType = $"weekly_batch_{dayOfWeek:D}";

        if (!await CanProcessBatchAsync(batchType, processDate))
        {
            _logger.LogInformation("Weekly batch for {Day} {Date} already processed, skipping", dayOfWeek, processDate);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var result = new BatchProcessingResult
        {
            BatchType = batchType,
            ProcessedDate = processDate
        };

        try
        {
            _logger.LogInformation("Starting weekly batch processing for {Day} {Date}", dayOfWeek, processDate);

            // Process weekly business reports
            await ProcessWeeklyBusinessReportsAsync(processDate, dayOfWeek, result);

            // Process weekly payout ready notifications for this day
            await ProcessWeeklyPayoutReadyAsync(processDate, dayOfWeek, result);

            stopwatch.Stop();
            result.ProcessingTime = stopwatch.Elapsed;

            await RecordBatchProcessingAsync(batchType, processDate, result);

            _logger.LogInformation("Completed weekly batch processing for {Day} {Date}: {Success}/{Total} successful, took {Duration}ms",
                dayOfWeek, processDate, result.SuccessfulSends, result.TotalTargets, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process weekly batch for {Day} {Date}", dayOfWeek, processDate);
            result.Errors.Add($"Fatal error: {ex.Message}");
            await RecordBatchProcessingAsync(batchType, processDate, result);
            throw;
        }
    }

    public async Task ProcessMonthlyBatchAsync(DateTime? forDate = null)
    {
        var processDate = forDate ?? DateTime.UtcNow.Date;
        var batchType = "monthly_batch";

        if (!await CanProcessBatchAsync(batchType, processDate))
        {
            _logger.LogInformation("Monthly batch for {Date} already processed, skipping", processDate);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var result = new BatchProcessingResult
        {
            BatchType = batchType,
            ProcessedDate = processDate
        };

        try
        {
            _logger.LogInformation("Starting monthly batch processing for {Date}", processDate);

            // Process monthly statements
            await ProcessMonthlyStatementsAsync(processDate, result);

            stopwatch.Stop();
            result.ProcessingTime = stopwatch.Elapsed;

            await RecordBatchProcessingAsync(batchType, processDate, result);

            _logger.LogInformation("Completed monthly batch processing for {Date}: {Success}/{Total} successful, took {Duration}ms",
                processDate, result.SuccessfulSends, result.TotalTargets, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process monthly batch for {Date}", processDate);
            result.Errors.Add($"Fatal error: {ex.Message}");
            await RecordBatchProcessingAsync(batchType, processDate, result);
            throw;
        }
    }

    public async Task<BatchDeliveryGroups> GetBatchDeliveryGroupsAsync(string notificationType, BatchFrequency frequency)
    {
        var groups = new BatchDeliveryGroups
        {
            NotificationType = notificationType,
            ProcessingDate = DateTime.UtcNow
        };

        try
        {
            // Get users who have this notification type enabled
            var preferences = await _context.NotificationPreferences
                .Include(np => np.User)
                .Where(np => np.TypePreferences.Contains(notificationType))
                .ToListAsync();

            foreach (var pref in preferences)
            {
                try
                {
                    var matrix = await _preferenceService.GetPreferencesAsync(pref.UserId, pref.Role);

                    // Check if notification is enabled for any channel
                    if (matrix.Preferences.TryGetValue(notificationType, out var channelPrefs) &&
                        channelPrefs.Any(c => c.Value))
                    {
                        var target = new BatchDeliveryTarget
                        {
                            UserId = pref.UserId,
                            Role = pref.Role,
                            Email = pref.User.Email ?? string.Empty,
                            PhoneNumber = matrix.PhoneNumber,
                            ChannelPreferences = channelPrefs,
                            PreferenceGroup = GetPreferenceGroup(matrix, notificationType, frequency)
                        };

                        if (!groups.Groups.ContainsKey(target.PreferenceGroup))
                            groups.Groups[target.PreferenceGroup] = new List<BatchDeliveryTarget>();

                        groups.Groups[target.PreferenceGroup].Add(target);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process batch delivery target for user {UserId}", pref.UserId);
                }
            }

            _logger.LogDebug("Created batch delivery groups for {NotificationType}: {GroupCount} groups, {TotalUsers} users",
                notificationType, groups.Groups.Count, groups.Groups.Values.Sum(g => g.Count));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get batch delivery groups for {NotificationType}", notificationType);
        }

        return groups;
    }

    public async Task<ConsolidatedReport> CreateConsolidatedReportAsync(Guid userId, string role, string reportType, DateTime periodStart, DateTime periodEnd)
    {
        var report = new ConsolidatedReport
        {
            UserId = userId,
            Role = role,
            ReportType = reportType,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            GeneratedAt = DateTime.UtcNow
        };

        try
        {
            switch (reportType)
            {
                case "daily_sales":
                    await GenerateDailySalesReport(report);
                    break;

                case "weekly_business":
                    await GenerateWeeklyBusinessReport(report);
                    break;

                case "weekly_payout":
                    await GenerateWeeklyPayoutReport(report);
                    break;

                case "monthly_statement":
                    await GenerateMonthlyStatementReport(report);
                    break;

                default:
                    throw new ArgumentException($"Unknown report type: {reportType}");
            }

            _logger.LogDebug("Generated {ReportType} report for user {UserId} covering {PeriodStart} to {PeriodEnd}",
                reportType, userId, periodStart, periodEnd);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create consolidated report {ReportType} for user {UserId}", reportType, userId);
            throw;
        }

        return report;
    }

    public async Task SendConsolidatedReportAsync(ConsolidatedReport report)
    {
        try
        {
            // Check if user can receive emails for this report type (this now uses bit-packed claims when available)
            var notificationType = MapReportTypeToNotification(report.ReportType);
            if (await _emailCompliance.CanSendEmailAsync(report.UserId, report.Role, notificationType))
            {
                // Try to get preferences from claims first, fallback to database
                var claimsPrefs = GetPackedPreferencesFromClaims(report.UserId, report.Role);
                NotificationPreferencesMatrix? preferences = null;

                if (!claimsPrefs.HasValue)
                {
                    // Fallback to database lookup when claims not available (typical for batch processing)
                    preferences = await _preferenceService.GetPreferencesAsync(report.UserId, report.Role);
                }

                await SendReportEmailAsync(report, preferences);
                _logger.LogInformation("Sent consolidated report {ReportType} to user {UserId} (claims: {UsesClaims})",
                    report.ReportType, report.UserId, claimsPrefs.HasValue);
            }
            else
            {
                _logger.LogDebug("Skipped sending report {ReportType} to user {UserId} - email notifications disabled",
                    report.ReportType, report.UserId);
            }

            // TODO: Add SMS support for report summaries if needed
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send consolidated report {ReportType} to user {UserId}", report.ReportType, report.UserId);
            throw;
        }
    }

    public async Task<bool> CanProcessBatchAsync(string batchType, DateTime forDate)
    {
        // TODO: Check processing log table to prevent duplicates
        // For now, return true but log the check
        _logger.LogDebug("Checking if batch {BatchType} can be processed for {Date}", batchType, forDate);

        // In production, this would query a BatchProcessingLog table:
        // return !await _context.BatchProcessingLogs
        //     .AnyAsync(log => log.BatchType == batchType && log.ProcessedDate == forDate && log.Status == "Completed");

        return true;
    }

    public async Task RecordBatchProcessingAsync(string batchType, DateTime forDate, BatchProcessingResult result)
    {
        try
        {
            // TODO: Store in BatchProcessingLog table
            _logger.LogInformation("Recording batch processing result: {BatchType} for {Date} - {Success}/{Total} successful in {Duration}ms",
                batchType, forDate, result.SuccessfulSends, result.TotalTargets, result.ProcessingTime.TotalMilliseconds);

            if (result.Errors.Any())
            {
                _logger.LogWarning("Batch processing had {ErrorCount} errors: {Errors}",
                    result.Errors.Count, string.Join("; ", result.Errors));
            }

            // In production, this would insert into a log table:
            // var logEntry = new BatchProcessingLog
            // {
            //     BatchType = batchType,
            //     ProcessedDate = forDate,
            //     Status = result.FailedSends == 0 ? "Completed" : "CompletedWithErrors",
            //     TotalTargets = result.TotalTargets,
            //     SuccessfulSends = result.SuccessfulSends,
            //     FailedSends = result.FailedSends,
            //     ProcessingTimeMs = (int)result.ProcessingTime.TotalMilliseconds,
            //     Errors = result.Errors.Any() ? JsonSerializer.Serialize(result.Errors) : null,
            //     CreatedAt = DateTime.UtcNow
            // };
            // _context.BatchProcessingLogs.Add(logEntry);
            // await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record batch processing result for {BatchType}", batchType);
        }
    }

    #region Private Report Generation Methods

    private async Task ProcessDailySalesSummaryAsync(DateTime processDate, BatchProcessingResult result)
    {
        var groups = await GetBatchDeliveryGroupsAsync("daily_sales_summary", BatchFrequency.Daily);

        foreach (var group in groups.Groups.Values)
        {
            foreach (var target in group)
            {
                try
                {
                    var periodStart = processDate.Date;
                    var periodEnd = periodStart.AddDays(1).AddTicks(-1);

                    var report = await CreateConsolidatedReportAsync(target.UserId, target.Role, "daily_sales", periodStart, periodEnd);
                    await SendConsolidatedReportAsync(report);

                    result.SuccessfulSends++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send daily sales summary to user {UserId}", target.UserId);
                    result.FailedSends++;
                    result.Errors.Add($"User {target.UserId}: {ex.Message}");
                }

                result.TotalTargets++;
            }
        }
    }

    private async Task ProcessDailyPayoutReadyAsync(DateTime processDate, BatchProcessingResult result)
    {
        var groups = await GetBatchDeliveryGroupsAsync("daily_payout_ready", BatchFrequency.Daily);

        foreach (var group in groups.Groups.Values)
        {
            foreach (var target in group)
            {
                try
                {
                    var periodStart = processDate.Date;
                    var periodEnd = periodStart.AddDays(1).AddTicks(-1);

                    var report = await CreateConsolidatedReportAsync(target.UserId, target.Role, "daily_payout", periodStart, periodEnd);
                    await SendConsolidatedReportAsync(report);

                    result.SuccessfulSends++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send daily payout ready to user {UserId}", target.UserId);
                    result.FailedSends++;
                    result.Errors.Add($"User {target.UserId}: {ex.Message}");
                }

                result.TotalTargets++;
            }
        }
    }

    private async Task ProcessWeeklyBusinessReportsAsync(DateTime processDate, DayOfWeek dayOfWeek, BatchProcessingResult result)
    {
        var groups = await GetBatchDeliveryGroupsAsync("weekly_report", BatchFrequency.Weekly);

        // Only process for users who have this day configured
        var dayGroups = groups.Groups.Where(g => g.Key.Contains(dayOfWeek.ToString().ToLower()));

        foreach (var group in dayGroups)
        {
            foreach (var target in group.Value)
            {
                try
                {
                    var weekStart = processDate.Date.AddDays(-(int)processDate.DayOfWeek);
                    var weekEnd = weekStart.AddDays(7).AddTicks(-1);

                    var report = await CreateConsolidatedReportAsync(target.UserId, target.Role, "weekly_business", weekStart, weekEnd);
                    await SendConsolidatedReportAsync(report);

                    result.SuccessfulSends++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send weekly business report to user {UserId}", target.UserId);
                    result.FailedSends++;
                    result.Errors.Add($"User {target.UserId}: {ex.Message}");
                }

                result.TotalTargets++;
            }
        }
    }

    private async Task ProcessWeeklyPayoutReadyAsync(DateTime processDate, DayOfWeek dayOfWeek, BatchProcessingResult result)
    {
        var groups = await GetBatchDeliveryGroupsAsync("weekly_payout_ready", BatchFrequency.Weekly);

        // Only process for users who have this day configured
        var dayGroups = groups.Groups.Where(g => g.Key.Contains(dayOfWeek.ToString().ToLower()));

        foreach (var group in dayGroups)
        {
            foreach (var target in group.Value)
            {
                try
                {
                    var weekStart = processDate.Date.AddDays(-(int)processDate.DayOfWeek);
                    var weekEnd = weekStart.AddDays(7).AddTicks(-1);

                    var report = await CreateConsolidatedReportAsync(target.UserId, target.Role, "weekly_payout", weekStart, weekEnd);
                    await SendConsolidatedReportAsync(report);

                    result.SuccessfulSends++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send weekly payout ready to user {UserId}", target.UserId);
                    result.FailedSends++;
                    result.Errors.Add($"User {target.UserId}: {ex.Message}");
                }

                result.TotalTargets++;
            }
        }
    }

    private async Task ProcessMonthlyStatementsAsync(DateTime processDate, BatchProcessingResult result)
    {
        var groups = await GetBatchDeliveryGroupsAsync("monthly_statement", BatchFrequency.Monthly);

        foreach (var group in groups.Groups.Values)
        {
            foreach (var target in group)
            {
                try
                {
                    var monthStart = new DateTime(processDate.Year, processDate.Month, 1);
                    var monthEnd = monthStart.AddMonths(1).AddTicks(-1);

                    var report = await CreateConsolidatedReportAsync(target.UserId, target.Role, "monthly_statement", monthStart, monthEnd);
                    await SendConsolidatedReportAsync(report);

                    result.SuccessfulSends++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send monthly statement to user {UserId}", target.UserId);
                    result.FailedSends++;
                    result.Errors.Add($"User {target.UserId}: {ex.Message}");
                }

                result.TotalTargets++;
            }
        }
    }

    private async Task GenerateDailySalesReport(ConsolidatedReport report)
    {
        // TODO: Query actual sales data for the period
        report.Title = $"Daily Sales Summary - {report.PeriodStart:MMM dd, yyyy}";
        report.HtmlContent = $"<h1>{report.Title}</h1><p>Your sales summary for {report.PeriodStart:MMM dd, yyyy}</p>";
        report.TextContent = $"{report.Title}\n\nYour sales summary for {report.PeriodStart:MMM dd, yyyy}";

        report.Sections.Add(new ReportSection
        {
            Title = "Sales Overview",
            Content = "Sales data would go here",
            Order = 1
        });
    }

    private async Task GenerateWeeklyBusinessReport(ConsolidatedReport report)
    {
        report.Title = $"Weekly Business Report - {report.PeriodStart:MMM dd} to {report.PeriodEnd:MMM dd, yyyy}";
        report.HtmlContent = $"<h1>{report.Title}</h1><p>Your weekly business overview</p>";
        report.TextContent = $"{report.Title}\n\nYour weekly business overview";

        report.Sections.Add(new ReportSection
        {
            Title = "Weekly Performance",
            Content = "Performance metrics would go here",
            Order = 1
        });
    }

    private async Task GenerateWeeklyPayoutReport(ConsolidatedReport report)
    {
        report.Title = $"Weekly Payout Ready - {report.PeriodStart:MMM dd} to {report.PeriodEnd:MMM dd, yyyy}";
        report.HtmlContent = $"<h1>{report.Title}</h1><p>Consignor payouts ready for processing</p>";
        report.TextContent = $"{report.Title}\n\nConsignor payouts ready for processing";

        report.Sections.Add(new ReportSection
        {
            Title = "Payout Summary",
            Content = "Payout details would go here",
            Order = 1
        });
    }

    private async Task GenerateMonthlyStatementReport(ConsolidatedReport report)
    {
        report.Title = $"Monthly Statement - {report.PeriodStart:MMM yyyy}";
        report.HtmlContent = $"<h1>{report.Title}</h1><p>Your comprehensive monthly business statement</p>";
        report.TextContent = $"{report.Title}\n\nYour comprehensive monthly business statement";

        report.Sections.Add(new ReportSection
        {
            Title = "Financial Summary",
            Content = "Financial details would go here",
            Order = 1
        });
    }

    private async Task SendReportEmailAsync(ConsolidatedReport report, NotificationPreferencesMatrix? preferences)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == report.UserId);
        if (user?.Email == null) return;

        var notificationType = MapReportTypeToNotification(report.ReportType);

        var complianceRequest = new EmailComplianceRequest
        {
            EmailContent = report.HtmlContent,
            EmailSubject = report.Title,
            UserId = report.UserId,
            Role = report.Role,
            NotificationType = notificationType,
            RecipientEmail = user.Email,
            BaseUrl = "https://app.consignmentgenie.com",
            OrganizationName = "ConsignmentGenie",
            SenderName = "ConsignmentGenie Reports",
            SenderEmail = "reports@consignmentgenie.com"
        };

        var complianceResult = await _emailCompliance.AddComplianceContentAsync(complianceRequest);

        // TODO: Send actual email
        _logger.LogInformation("Would send consolidated report email to {Email}: {Subject}", user.Email, report.Title);

        await _emailCompliance.RecordEmailSentAsync(report.UserId, report.Role, notificationType, user.Email);
    }

    private static string GetPreferenceGroup(NotificationPreferencesMatrix matrix, string notificationType, BatchFrequency frequency)
    {
        if (frequency == BatchFrequency.Weekly && notificationType == "weekly_payout_ready")
        {
            return matrix.BatchPreferences.TryGetValue("weekly_payout_ready", out var day) ? day : "monday";
        }

        return frequency.ToString().ToLower();
    }

    private static string MapReportTypeToNotification(string reportType)
    {
        return reportType switch
        {
            "daily_sales" => "daily_sales_summary",
            "weekly_business" => "weekly_report",
            "weekly_payout" => "weekly_payout_ready",
            "monthly_statement" => "monthly_statement",
            _ => reportType
        };
    }

    /// <summary>
    /// Extracts bit-packed notification preferences from JWT claims for ultra-fast lookup
    /// Note: This is typically null for batch processing since background jobs run without user context
    /// </summary>
    private long? GetPackedPreferencesFromClaims(Guid userId, string role)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            // Build the claim name: notification_prefs_{role}
            var claimName = $"notification_prefs_{role.ToLowerInvariant()}";
            var claimValue = httpContext.User.FindFirst(claimName)?.Value;

            if (string.IsNullOrEmpty(claimValue))
            {
                return null;
            }

            // Verify the claim is for the correct user (security check)
            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim != userId.ToString())
            {
                _logger.LogWarning("User ID mismatch: token contains {TokenUserId}, requested {RequestedUserId}",
                    userIdClaim, userId);
                return null;
            }

            // Parse the packed preferences from the claim
            if (long.TryParse(claimValue, out var packedPrefs))
            {
                _logger.LogDebug("Retrieved packed preferences for user {UserId}, role {Role}: {PackedValue}",
                    userId, role, packedPrefs);
                return packedPrefs;
            }

            _logger.LogWarning("Invalid packed preferences format in claim {ClaimName}: {ClaimValue}",
                claimName, claimValue);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract packed preferences from claims for user {UserId}, role {Role}",
                userId, role);
            return null;
        }
    }

    #endregion
}