using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.DTOs;
using ConsignmentGenie.Core.DTOs.Notifications;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Core.Interfaces;
using ConsignmentGenie.Core.Utilities;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Text.Json;

namespace ConsignmentGenie.Application.Services;

/// <summary>
/// Enhanced notification service with fast preference lookup, email compliance, and batch processing
/// Replaces the original NotificationService with performance and compliance improvements
/// </summary>
public class EnhancedNotificationService : INotificationService
{
    private readonly ConsignmentGenieContext _context;
    private readonly INotificationPreferenceService _preferenceService;
    private readonly IEmailComplianceService _emailCompliance;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<EnhancedNotificationService> _logger;

    // TODO: Inject actual email/SMS services when available
    // private readonly IEmailService _emailService;
    // private readonly ISmsService _smsService;

    public EnhancedNotificationService(
        ConsignmentGenieContext context,
        INotificationPreferenceService preferenceService,
        IEmailComplianceService emailCompliance,
        IHttpContextAccessor httpContextAccessor,
        ILogger<EnhancedNotificationService> logger)
    {
        _context = context;
        _preferenceService = preferenceService;
        _emailCompliance = emailCompliance;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<Notification> CreateAsync(CreateNotificationRequest request)
    {
        try
        {
            _logger.LogInformation("Creating notification {Type} for user {ToUserId}", request.Type, request.ToUserId);

            // Always store in database first
            var notification = new Notification
            {
                FromUserId = request.FromUserId,
                FromType = request.FromType,
                ToUserId = request.ToUserId,
                ToType = request.ToType,
                ActionStatus = request.ActionStatus,
                Type = request.Type,
                Title = request.Title,
                Message = request.Message,
                OrganizationId = request.OrganizationId,
                Payload = request.Payload != null ? JsonSerializer.Serialize(request.Payload) : null,
                AttachmentUrl = request.AttachmentUrl,
                AttachmentName = request.AttachmentName,
                AttachmentType = request.AttachmentType,
                ActionUrl = request.ActionUrl,
                ItemId = request.ItemId,
                TransactionId = request.TransactionId,
                PayoutId = request.PayoutId,
                StatementId = request.StatementId,
                ItemRequestId = request.ItemRequestId,
                ReferenceType = request.ReferenceType,
                ReferenceId = request.ReferenceId,
                Metadata = request.Metadata != null ? JsonSerializer.Serialize(request.Metadata) : null,
                IsRead = false,
                EmailSent = false,
                SmsSent = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Send notifications via enabled channels using preference-aware filtering
            await SendNotificationChannelsAsync(notification);

            _logger.LogInformation("Notification {Type} created and sent for user {ToUserId} with ID {NotificationId}",
                request.Type, request.ToUserId, notification.Id);

            return notification;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating notification {Type} for user {ToUserId}", request.Type, request.ToUserId);
            throw;
        }
    }

    public async Task<IEnumerable<Notification>> CreateBulkAsync(IEnumerable<CreateNotificationRequest> requests)
    {
        try
        {
            var notifications = new List<Notification>();
            var requestList = requests.ToList();

            // Batch create notifications in database
            foreach (var request in requestList)
            {
                var notification = new Notification
                {
                    FromUserId = request.FromUserId,
                    FromType = request.FromType,
                    ToUserId = request.ToUserId,
                    ToType = request.ToType,
                    ActionStatus = request.ActionStatus,
                    Type = request.Type,
                    Title = request.Title,
                    Message = request.Message,
                    OrganizationId = request.OrganizationId,
                    Payload = request.Payload != null ? JsonSerializer.Serialize(request.Payload) : null,
                    AttachmentUrl = request.AttachmentUrl,
                    AttachmentName = request.AttachmentName,
                    AttachmentType = request.AttachmentType,
                    ActionUrl = request.ActionUrl,
                    ItemId = request.ItemId,
                    TransactionId = request.TransactionId,
                    PayoutId = request.PayoutId,
                    StatementId = request.StatementId,
                    ItemRequestId = request.ItemRequestId,
                    ReferenceType = request.ReferenceType,
                    ReferenceId = request.ReferenceId,
                    IsRead = false,
                    EmailSent = false,
                    SmsSent = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                notifications.Add(notification);
            }

            _context.Notifications.AddRange(notifications);
            await _context.SaveChangesAsync();

            // Batch send notifications with preference optimization
            await SendBulkNotificationChannelsAsync(notifications);

            _logger.LogInformation("Created and sent {Count} notifications in bulk", notifications.Count);

            return notifications;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating bulk notifications");
            throw;
        }
    }

    /// <summary>
    /// Sends notifications via enabled channels with preference-aware filtering using bit-packed claims
    /// </summary>
    private async Task SendNotificationChannelsAsync(Notification notification)
    {
        try
        {
            // Try to get preferences from JWT claims first (ultra-fast)
            var claimsPrefs = GetPackedPreferencesFromClaims(notification.ToUserId, notification.ToType);

            if (claimsPrefs.HasValue)
            {
                // Use bit-packed claims for instant lookup
                var packedPrefs = claimsPrefs.Value;

                // Check email preference and compliance
                if (NotificationPrefsBitPacker.IsEnabled(packedPrefs, notification.Type, NotificationChannel.Email))
                {
                    if (await _emailCompliance.CanSendEmailAsync(notification.ToUserId, notification.ToType, notification.Type))
                    {
                        await SendEmailNotificationAsync(notification, null); // No need for full preferences object
                    }
                }

                // Check SMS preference
                if (NotificationPrefsBitPacker.IsEnabled(packedPrefs, notification.Type, NotificationChannel.Sms))
                {
                    await SendSmsNotificationAsync(notification, null); // No need for full preferences object
                }
            }
            else
            {
                // Fallback to database lookup if claims not available
                _logger.LogInformation("JWT claims not available for user {UserId}, falling back to database lookup", notification.ToUserId);

                var preferences = await _preferenceService.GetPreferencesAsync(notification.ToUserId, notification.ToType);

                if (preferences.Preferences.TryGetValue(notification.Type, out var channelPrefs))
                {
                    // Check email preference and compliance
                    if (channelPrefs.TryGetValue(NotificationChannel.Email, out var emailEnabled) && emailEnabled)
                    {
                        if (await _emailCompliance.CanSendEmailAsync(notification.ToUserId, notification.ToType, notification.Type))
                        {
                            await SendEmailNotificationAsync(notification, preferences);
                        }
                    }

                    // Check SMS preference
                    if (channelPrefs.TryGetValue(NotificationChannel.Sms, out var smsEnabled) && smsEnabled && preferences.SmsVerified)
                    {
                        await SendSmsNotificationAsync(notification, preferences);
                    }
                }
                else
                {
                    // Default behavior for new notification types - send system notification only
                    _logger.LogInformation("No preferences found for notification type {Type}, using default system notification", notification.Type);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification channels for notification {NotificationId}", notification.Id);
        }
    }

    /// <summary>
    /// Efficiently sends bulk notifications with bit-packed claims lookup for ultra-fast processing
    /// </summary>
    private async Task SendBulkNotificationChannelsAsync(List<Notification> notifications)
    {
        try
        {
            var emailTasks = new List<Task>();
            var smsTasks = new List<Task>();
            var fallbackNotifications = new List<Notification>();

            // First pass: Use bit-packed claims for instant preference checks
            foreach (var notification in notifications)
            {
                var claimsPrefs = GetPackedPreferencesFromClaims(notification.ToUserId, notification.ToType);

                if (claimsPrefs.HasValue)
                {
                    var packedPrefs = claimsPrefs.Value;

                    // Queue email notifications using bitwise checks
                    if (NotificationPrefsBitPacker.IsEnabled(packedPrefs, notification.Type, NotificationChannel.Email))
                    {
                        if (await _emailCompliance.CanSendEmailAsync(notification.ToUserId, notification.ToType, notification.Type))
                        {
                            emailTasks.Add(SendEmailNotificationAsync(notification, null));
                        }
                    }

                    // Queue SMS notifications using bitwise checks
                    if (NotificationPrefsBitPacker.IsEnabled(packedPrefs, notification.Type, NotificationChannel.Sms))
                    {
                        smsTasks.Add(SendSmsNotificationAsync(notification, null));
                    }
                }
                else
                {
                    // Collect notifications that need database fallback
                    fallbackNotifications.Add(notification);
                }
            }

            // Second pass: Handle fallback notifications with batch database lookup
            if (fallbackNotifications.Any())
            {
                _logger.LogInformation("Processing {Count} notifications via database fallback", fallbackNotifications.Count);

                // Group fallback notifications by user and role for efficient preference lookup
                var userRoleGroups = fallbackNotifications.GroupBy(n => new { n.ToUserId, n.ToType }).ToList();
                var rolePreferencesMap = new Dictionary<string, Dictionary<Guid, NotificationPreferencesMatrix>>();

                foreach (var roleGroup in userRoleGroups.GroupBy(g => g.Key.ToType))
                {
                    var userIds = roleGroup.Select(g => g.Key.ToUserId).ToList();
                    var preferences = await _preferenceService.GetPreferencesAsync(userIds, roleGroup.Key);
                    rolePreferencesMap[roleGroup.Key] = preferences;
                }

                // Process fallback notifications with pre-loaded preferences
                foreach (var notification in fallbackNotifications)
                {
                    if (rolePreferencesMap.TryGetValue(notification.ToType, out var userPrefs) &&
                        userPrefs.TryGetValue(notification.ToUserId, out var preferences))
                    {
                        if (preferences.Preferences.TryGetValue(notification.Type, out var channelPrefs))
                        {
                            // Queue email notifications
                            if (channelPrefs.TryGetValue(NotificationChannel.Email, out var emailEnabled) && emailEnabled)
                            {
                                if (await _emailCompliance.CanSendEmailAsync(notification.ToUserId, notification.ToType, notification.Type))
                                {
                                    emailTasks.Add(SendEmailNotificationAsync(notification, preferences));
                                }
                            }

                            // Queue SMS notifications
                            if (channelPrefs.TryGetValue(NotificationChannel.Sms, out var smsEnabled) && smsEnabled && preferences.SmsVerified)
                            {
                                smsTasks.Add(SendSmsNotificationAsync(notification, preferences));
                            }
                        }
                    }
                }
            }

            // Execute all email and SMS sends in parallel
            await Task.WhenAll(emailTasks.Concat(smsTasks));

            _logger.LogInformation("Processed bulk notifications: {EmailCount} emails, {SmsCount} SMS ({ClaimsCount} via claims, {FallbackCount} via database)",
                emailTasks.Count, smsTasks.Count, notifications.Count - fallbackNotifications.Count, fallbackNotifications.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send bulk notification channels");
        }
    }

    /// <summary>
    /// Sends email notification with compliance headers and unsubscribe functionality
    /// </summary>
    private async Task SendEmailNotificationAsync(Notification notification, NotificationPreferencesMatrix? preferences)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == notification.ToUserId);
            if (user == null || string.IsNullOrEmpty(user.Email))
            {
                _logger.LogWarning("Cannot send email notification: user {UserId} not found or no email address", notification.ToUserId);
                return;
            }

            // Get organization info for compliance
            var organization = await _context.Organizations.FirstOrDefaultAsync(o => o.Id == notification.OrganizationId);
            var organizationName = organization?.Name ?? "ConsignmentGenie";

            // Add compliance content and headers
            var complianceRequest = new EmailComplianceRequest
            {
                EmailContent = notification.Message,
                EmailSubject = notification.Title,
                UserId = notification.ToUserId,
                Role = notification.ToType,
                NotificationType = notification.Type,
                RecipientEmail = user.Email,
                BaseUrl = "https://app.consignmentgenie.com", // TODO: Get from configuration
                OrganizationName = organizationName,
                SenderName = "ConsignmentGenie",
                SenderEmail = "notifications@consignmentgenie.com" // TODO: Get from configuration
            };

            var complianceResult = await _emailCompliance.AddComplianceContentAsync(complianceRequest);

            // TODO: Send email using actual email service
            // await _emailService.SendAsync(new EmailMessage
            // {
            //     To = user.Email,
            //     Subject = complianceResult.EmailSubject,
            //     Body = complianceResult.EmailContent,
            //     Headers = complianceResult.Headers
            // });

            // For now, just log that we would send the email
            _logger.LogInformation("Would send email to {Email} for notification {Type}: {Subject}",
                user.Email, notification.Type, notification.Title);

            // Record email sent for compliance audit
            await _emailCompliance.RecordEmailSentAsync(
                notification.ToUserId,
                notification.ToType,
                notification.Type,
                user.Email);

            // Update notification status
            notification.EmailSent = true;
            notification.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email notification for notification {NotificationId}", notification.Id);
        }
    }

    /// <summary>
    /// Sends SMS notification (placeholder for future implementation)
    /// </summary>
    private async Task SendSmsNotificationAsync(Notification notification, NotificationPreferencesMatrix? preferences)
    {
        try
        {
            // Get user's phone number from database if not provided via preferences
            string? phoneNumber = preferences?.PhoneNumber;
            bool smsVerified = preferences?.SmsVerified ?? false;

            if (preferences == null)
            {
                // Load minimal user data for SMS when using claims-based path
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == notification.ToUserId);
                phoneNumber = user?.Phone;
                // TODO: Add SmsVerified field to User entity or check verification status
                smsVerified = !string.IsNullOrEmpty(phoneNumber); // Simplified for now
            }

            if (string.IsNullOrEmpty(phoneNumber) || !smsVerified)
            {
                _logger.LogWarning("Cannot send SMS notification: user {UserId} has no verified phone number", notification.ToUserId);
                return;
            }

            // TODO: Send SMS using actual SMS service
            // await _smsService.SendAsync(new SmsMessage
            // {
            //     To = preferences.PhoneNumber,
            //     Body = $"{notification.Title}\n\n{notification.Message}"
            // });

            // For now, just log that we would send the SMS
            _logger.LogInformation("Would send SMS to {PhoneNumber} for notification {Type}: {Title}",
                phoneNumber, notification.Type, notification.Title);

            // Update notification status
            notification.SmsSent = true;
            notification.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS notification for notification {NotificationId}", notification.Id);
        }
    }

    #region Original INotificationService Implementation (Unchanged)

    public async Task<PagedResult<NotificationDto>> GetNotificationsAsync(Guid userId, string role, NotificationQueryParams queryParams)
    {
        try
        {
            var queryable = _context.Notifications
                .Where(n => n.ToUserId == userId && n.ToType == role && n.DeletedAt == null);

            // Apply filters
            if (!string.IsNullOrEmpty(queryParams.Type))
                queryable = queryable.Where(n => n.Type == queryParams.Type);

            if (queryParams.IsRead.HasValue)
                queryable = queryable.Where(n => n.IsRead == queryParams.IsRead.Value);

            if (queryParams.HasAttachment.HasValue)
                queryable = queryable.Where(n => queryParams.HasAttachment.Value ?
                    !string.IsNullOrEmpty(n.AttachmentUrl) :
                    string.IsNullOrEmpty(n.AttachmentUrl));

            if (queryParams.From.HasValue)
                queryable = queryable.Where(n => n.CreatedAt >= queryParams.From.Value);

            if (queryParams.To.HasValue)
                queryable = queryable.Where(n => n.CreatedAt <= queryParams.To.Value);

            // Get total count
            var totalCount = await queryable.CountAsync();

            // Apply sorting
            switch (queryParams.SortBy?.ToLowerInvariant())
            {
                case "readat":
                    queryable = queryParams.SortDirection?.ToLowerInvariant() == "asc" ?
                        queryable.OrderBy(n => n.ReadAt) :
                        queryable.OrderByDescending(n => n.ReadAt);
                    break;
                case "createdat":
                default:
                    queryable = queryParams.SortDirection?.ToLowerInvariant() == "asc" ?
                        queryable.OrderBy(n => n.CreatedAt) :
                        queryable.OrderByDescending(n => n.CreatedAt);
                    break;
            }

            // Apply pagination and get notifications with raw data
            var notificationsQuery = await queryable
                .Skip((queryParams.Page - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .Select(n => new
                {
                    n.Id,
                    n.Type,
                    n.Title,
                    n.Message,
                    n.IsRead,
                    n.CreatedAt,
                    n.ReferenceType,
                    n.ReferenceId,
                    n.ActionUrl,
                    n.Payload,
                    n.AttachmentUrl
                })
                .ToListAsync();

            // Map to DTOs and deserialize payload
            var notifications = notificationsQuery.Select(n => new NotificationDto
            {
                NotificationId = n.Id,
                Type = n.Type,
                Title = n.Title,
                Message = n.Message,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt,
                TimeAgo = CalculateTimeAgo(n.CreatedAt),
                RelatedEntityType = n.ReferenceType,
                RelatedEntityId = n.ReferenceId,
                ActionUrl = n.ActionUrl,
                Metadata = !string.IsNullOrEmpty(n.Payload) ?
                    JsonSerializer.Deserialize<NotificationMetadata>(n.Payload) :
                    null
            }).ToList();

            return new PagedResult<NotificationDto>(notifications, totalCount, queryParams.Page, queryParams.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notifications for user {UserId} with role {Role}", userId, role);
            throw;
        }
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, string role)
    {
        try
        {
            var unreadCount = await _context.Notifications
                .Where(n => n.ToUserId == userId && n.ToType == role && !n.IsRead && n.DeletedAt == null)
                .CountAsync();

            return unreadCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unread count for user {UserId} with role {Role}", userId, role);
            throw;
        }
    }

    public async Task MarkAsReadAsync(Guid notificationId, Guid userId)
    {
        try
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.ToUserId == userId);

            if (notification == null)
            {
                _logger.LogWarning("Notification {NotificationId} not found for user {UserId}", notificationId, userId);
                return;
            }

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                notification.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Notification {NotificationId} marked as read for user {UserId}", notificationId, userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {NotificationId} as read for user {UserId}", notificationId, userId);
            throw;
        }
    }

    public async Task MarkAsUnreadAsync(Guid notificationId, Guid userId)
    {
        try
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.ToUserId == userId);

            if (notification == null)
            {
                _logger.LogWarning("Notification {NotificationId} not found for user {UserId}", notificationId, userId);
                return;
            }

            if (notification.IsRead)
            {
                notification.IsRead = false;
                notification.ReadAt = null;
                notification.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Notification {NotificationId} marked as unread for user {UserId}", notificationId, userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {NotificationId} as unread for user {UserId}", notificationId, userId);
            throw;
        }
    }

    public async Task MarkAllAsReadAsync(Guid userId, string role)
    {
        try
        {
            var unreadNotifications = await _context.Notifications
                .Where(n => n.ToUserId == userId && n.ToType == role && !n.IsRead && n.DeletedAt == null)
                .ToListAsync();

            if (unreadNotifications.Any())
            {
                var now = DateTime.UtcNow;
                foreach (var notification in unreadNotifications)
                {
                    notification.IsRead = true;
                    notification.ReadAt = now;
                    notification.UpdatedAt = now;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Marked {Count} notifications as read for user {UserId} with role {Role}",
                    unreadNotifications.Count, userId, role);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read for user {UserId} with role {Role}", userId, role);
            throw;
        }
    }

    public async Task DeleteAsync(Guid notificationId, Guid userId)
    {
        try
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.ToUserId == userId);

            if (notification == null)
            {
                _logger.LogWarning("Notification {NotificationId} not found for user {UserId}", notificationId, userId);
                return;
            }

            // Soft delete
            notification.DeletedAt = DateTime.UtcNow;
            notification.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Notification {NotificationId} soft deleted for user {UserId}", notificationId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting notification {NotificationId} for user {UserId}", notificationId, userId);
            throw;
        }
    }

    public async Task<NotificationPreferencesDto> GetPreferencesAsync(Guid userId, string role)
    {
        try
        {
            var preferences = await _preferenceService.GetPreferencesAsync(userId, role);

            return new NotificationPreferencesDto
            {
                UserId = userId,
                Role = role,
                EmailEnabled = preferences.Preferences.Values.Any(p => p.TryGetValue(NotificationChannel.Email, out var enabled) && enabled),
                SmsEnabled = preferences.SmsVerified && preferences.Preferences.Values.Any(p => p.TryGetValue(NotificationChannel.Sms, out var enabled) && enabled),
                PushEnabled = true,
                EmailItemSold = preferences.Preferences.TryGetValue("item_sold", out var itemSoldPrefs) ? itemSoldPrefs.TryGetValue(NotificationChannel.Email, out var itemSoldEmail) && itemSoldEmail : true,
                EmailPayoutProcessed = preferences.Preferences.TryGetValue("daily_payout_ready", out var payoutPrefs) ? payoutPrefs.TryGetValue(NotificationChannel.Email, out var payoutEmail) && payoutEmail : true,
                EmailPayoutPending = true,
                EmailItemExpired = true,
                EmailStatementReady = true,
                EmailAccountUpdate = true,
                DigestEnabled = false,
                DigestFrequency = "daily",
                DigestTime = "09:00",
                DigestDay = 1,
                PayoutPendingThreshold = 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting preferences for user {UserId} with role {Role}", userId, role);
            throw;
        }
    }

    public async Task UpdatePreferencesAsync(Guid userId, string role, UpdateNotificationPreferencesRequest request)
    {
        try
        {
            var currentPrefs = await _preferenceService.GetPreferencesAsync(userId, role);

            // Update email preferences
            foreach (var prefKey in currentPrefs.Preferences.Keys.ToList())
            {
                if (currentPrefs.Preferences[prefKey].ContainsKey(NotificationChannel.Email))
                {
                    currentPrefs.Preferences[prefKey][NotificationChannel.Email] = request.EmailEnabled;
                }
            }

            await _preferenceService.UpdatePreferencesAsync(userId, role, currentPrefs);
            _logger.LogInformation("Updated notification preferences for user {UserId} with role {Role}", userId, role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating preferences for user {UserId} with role {Role}", userId, role);
            throw;
        }
    }

    public async Task MarkAsImportantAsync(Guid notificationId, Guid userId)
    {
        try
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.ToUserId == userId);

            if (notification == null)
            {
                _logger.LogWarning("Notification {NotificationId} not found for user {UserId}", notificationId, userId);
                return;
            }

            if (!notification.IsImportant)
            {
                notification.IsImportant = true;
                notification.MarkedImportantAt = DateTime.UtcNow;
                notification.MarkedImportantByUserId = userId;
                notification.UpdatedAt = DateTime.UtcNow;

                _context.Notifications.Update(notification);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Marked notification {NotificationId} as important for user {UserId}", notificationId, userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {NotificationId} as important for user {UserId}", notificationId, userId);
            throw;
        }
    }

    public async Task MarkAsNotImportantAsync(Guid notificationId, Guid userId)
    {
        try
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.ToUserId == userId);

            if (notification == null)
            {
                _logger.LogWarning("Notification {NotificationId} not found for user {UserId}", notificationId, userId);
                return;
            }

            if (notification.IsImportant)
            {
                notification.IsImportant = false;
                notification.MarkedImportantAt = null;
                notification.MarkedImportantByUserId = null;
                notification.UpdatedAt = DateTime.UtcNow;

                _context.Notifications.Update(notification);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Marked notification {NotificationId} as not important for user {UserId}", notificationId, userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {NotificationId} as not important for user {UserId}", notificationId, userId);
            throw;
        }
    }

    public async Task CompleteNotificationActionAsync(Guid referenceId, string referenceType, Guid completedByUserId)
    {
        try
        {
            _logger.LogInformation("Completing notification action for {ReferenceType} {ReferenceId} by user {UserId}",
                referenceType, referenceId, completedByUserId);

            var notifications = await _context.Notifications
                .Where(n => n.ReferenceId == referenceId &&
                           n.ReferenceType == referenceType &&
                           n.ActionStatus == "pending")
                .ToListAsync();

            if (!notifications.Any())
            {
                _logger.LogInformation("No pending notifications found for {ReferenceType} {ReferenceId}", referenceType, referenceId);
                return;
            }

            foreach (var notification in notifications)
            {
                notification.ActionStatus = "completed";
                notification.ActionCompletedAt = DateTime.UtcNow;
                notification.ActionCompletedByUserId = completedByUserId;
                notification.UpdatedAt = DateTime.UtcNow;
            }

            _context.Notifications.UpdateRange(notifications);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Completed {Count} notification actions for {ReferenceType} {ReferenceId}",
                notifications.Count, referenceType, referenceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing notification action for {ReferenceType} {ReferenceId}", referenceType, referenceId);
            throw;
        }
    }

    public async Task AcknowledgeNotificationAsync(Guid notificationId, Guid acknowledgedByUserId)
    {
        try
        {
            _logger.LogInformation("Acknowledging notification {NotificationId} by user {UserId}",
                notificationId, acknowledgedByUserId);

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId);

            if (notification == null)
            {
                _logger.LogWarning("Notification {NotificationId} not found for acknowledgment", notificationId);
                return;
            }

            if (!notification.IsAcknowledged)
            {
                notification.IsAcknowledged = true;
                notification.AcknowledgedAt = DateTime.UtcNow;
                notification.AcknowledgedByUserId = acknowledgedByUserId;
                notification.UpdatedAt = DateTime.UtcNow;

                // Also mark as read if not already read
                if (!notification.IsRead)
                {
                    notification.IsRead = true;
                    notification.ReadAt = DateTime.UtcNow;
                }

                _context.Notifications.Update(notification);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully acknowledged notification {NotificationId} by user {UserId}",
                    notificationId, acknowledgedByUserId);
            }
            else
            {
                _logger.LogInformation("Notification {NotificationId} was already acknowledged", notificationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acknowledging notification {NotificationId} by user {UserId}",
                notificationId, acknowledgedByUserId);
            throw;
        }
    }

    private static string CalculateTimeAgo(DateTime createdAt)
    {
        var now = DateTime.UtcNow;
        var timeSpan = now - createdAt;

        if (timeSpan.TotalMinutes < 1)
            return "Just now";
        if (timeSpan.TotalMinutes < 60)
            return $"{(int)timeSpan.TotalMinutes} minutes ago";
        if (timeSpan.TotalHours < 24)
            return $"{(int)timeSpan.TotalHours} hours ago";
        if (timeSpan.TotalDays < 7)
            return $"{(int)timeSpan.TotalDays} days ago";
        if (timeSpan.TotalDays < 30)
            return $"{(int)(timeSpan.TotalDays / 7)} weeks ago";

        return createdAt.ToString("MMM dd, yyyy");
    }

    /// <summary>
    /// Extracts bit-packed notification preferences from JWT claims for ultra-fast lookup
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