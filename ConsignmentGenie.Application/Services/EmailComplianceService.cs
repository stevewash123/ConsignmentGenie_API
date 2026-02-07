using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.DTOs.Notifications;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Core.Utilities;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ConsignmentGenie.Application.Services;

/// <summary>
/// Email compliance service for CAN-SPAM and GDPR compliance
/// Generates secure unsubscribe tokens linked to notification preference matrix
/// </summary>
public class EmailComplianceService : IEmailComplianceService
{
    private readonly ConsignmentGenieContext _context;
    private readonly INotificationPreferenceService _preferenceService;
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<EmailComplianceService> _logger;

    private readonly string _secretKey;
    private static readonly TimeSpan TokenExpiry = TimeSpan.FromDays(90); // 90 days for unsubscribe tokens

    public EmailComplianceService(
        ConsignmentGenieContext context,
        INotificationPreferenceService preferenceService,
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor,
        ILogger<EmailComplianceService> logger)
    {
        _context = context;
        _preferenceService = preferenceService;
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;

        _secretKey = _configuration["EmailCompliance:SecretKey"] ??
                    _configuration["Jwt:Key"] ??
                    throw new InvalidOperationException("No secret key configured for email compliance");
    }

    public async Task<string> GenerateUnsubscribeTokenAsync(Guid userId, string role, string notificationType)
    {
        var tokenData = new
        {
            userId = userId.ToString(),
            role = role,
            notificationType = notificationType,
            expiresAt = DateTimeOffset.UtcNow.Add(TokenExpiry).ToUnixTimeSeconds()
        };

        var json = JsonSerializer.Serialize(tokenData);
        var encryptedToken = EncryptString(json, _secretKey);

        _logger.LogDebug("Generated unsubscribe token for user {UserId}, role {Role}, notification {NotificationType}",
            userId, role, notificationType);

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(encryptedToken));
    }

    public async Task<UnsubscribeTokenInfo?> ValidateUnsubscribeTokenAsync(string token)
    {
        try
        {
            var encryptedData = Encoding.UTF8.GetString(Convert.FromBase64String(token));
            var json = DecryptString(encryptedData, _secretKey);
            var tokenData = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

            if (tokenData == null) return null;

            var expiresAt = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(tokenData["expiresAt"])).DateTime;

            return new UnsubscribeTokenInfo
            {
                UserId = Guid.Parse(tokenData["userId"].ToString()!),
                Role = tokenData["role"].ToString()!,
                NotificationType = tokenData["notificationType"].ToString()!,
                ExpiresAt = expiresAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to validate unsubscribe token");
            return null;
        }
    }

    public async Task<UnsubscribeResult> ProcessUnsubscribeAsync(string token, UnsubscribeScope scope = UnsubscribeScope.SpecificNotification)
    {
        var tokenInfo = await ValidateUnsubscribeTokenAsync(token);
        if (tokenInfo == null)
        {
            return new UnsubscribeResult { Success = false, Message = "Invalid or expired unsubscribe token" };
        }

        if (tokenInfo.IsExpired)
        {
            return new UnsubscribeResult { Success = false, Message = "Unsubscribe token has expired" };
        }

        try
        {
            var preferences = await _preferenceService.GetPreferencesAsync(tokenInfo.UserId, tokenInfo.Role);
            var updatedTypes = new List<string>();

            switch (scope)
            {
                case UnsubscribeScope.SpecificNotification:
                    if (preferences.Preferences.ContainsKey(tokenInfo.NotificationType))
                    {
                        preferences.Preferences[tokenInfo.NotificationType][NotificationChannel.Email] = false;
                        updatedTypes.Add(tokenInfo.NotificationType);
                    }
                    break;

                case UnsubscribeScope.CategoryNotifications:
                    var category = GetNotificationCategory(tokenInfo.NotificationType);
                    foreach (var type in GetNotificationTypesInCategory(category))
                    {
                        if (preferences.Preferences.ContainsKey(type))
                        {
                            preferences.Preferences[type][NotificationChannel.Email] = false;
                            updatedTypes.Add(type);
                        }
                    }
                    break;

                case UnsubscribeScope.AllEmailNotifications:
                    foreach (var prefKey in preferences.Preferences.Keys.ToList())
                    {
                        preferences.Preferences[prefKey][NotificationChannel.Email] = false;
                        updatedTypes.Add(prefKey);
                    }
                    break;
            }

            await _preferenceService.UpdatePreferencesAsync(tokenInfo.UserId, tokenInfo.Role, preferences);

            // Record the unsubscribe event for audit
            await RecordUnsubscribeEventAsync(tokenInfo.UserId, tokenInfo.Role, tokenInfo.NotificationType, scope);

            _logger.LogInformation("Processed unsubscribe for user {UserId}, scope {Scope}, updated {Count} notification types",
                tokenInfo.UserId, scope, updatedTypes.Count);

            return new UnsubscribeResult
            {
                Success = true,
                Message = GetUnsubscribeSuccessMessage(scope, updatedTypes.Count),
                ProcessedScope = scope,
                UpdatedNotificationTypes = updatedTypes
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process unsubscribe for user {UserId}", tokenInfo.UserId);
            return new UnsubscribeResult { Success = false, Message = "Failed to process unsubscribe request" };
        }
    }

    public string GetUnsubscribeUrl(string baseUrl, string token)
    {
        return $"{baseUrl.TrimEnd('/')}/unsubscribe?token={Uri.EscapeDataString(token)}";
    }

    public async Task<EmailComplianceResult> AddComplianceContentAsync(EmailComplianceRequest request)
    {
        var token = await GenerateUnsubscribeTokenAsync(request.UserId, request.Role, request.NotificationType);
        var unsubscribeUrl = GetUnsubscribeUrl(request.BaseUrl, token);

        // Add compliance footer
        var complianceFooter = GenerateComplianceFooter(request, unsubscribeUrl);
        var compliantContent = request.EmailContent + "\n\n" + complianceFooter;

        // Add required headers
        var headers = new Dictionary<string, string>
        {
            ["List-Unsubscribe"] = $"<{unsubscribeUrl}>",
            ["List-Unsubscribe-Post"] = "List-Unsubscribe=One-Click",
            ["X-Email-Type"] = request.NotificationType,
            ["X-Organization"] = request.OrganizationName
        };

        return new EmailComplianceResult
        {
            EmailContent = compliantContent,
            EmailSubject = request.EmailSubject,
            UnsubscribeToken = token,
            UnsubscribeUrl = unsubscribeUrl,
            Headers = headers
        };
    }

    public async Task<bool> CanSendEmailAsync(Guid userId, string role, string notificationType)
    {
        // Try to use bit-packed claims first (ultra-fast)
        var claimsPrefs = GetPackedPreferencesFromClaims(userId, role);

        if (claimsPrefs.HasValue)
        {
            // Use bitwise check for instant result
            return NotificationPrefsBitPacker.IsEnabled(claimsPrefs.Value, notificationType, NotificationChannel.Email);
        }

        // Fallback to database lookup if claims not available
        return await _preferenceService.IsNotificationEnabledAsync(userId, role, notificationType, NotificationChannel.Email);
    }

    public async Task RecordEmailSentAsync(Guid userId, string role, string notificationType, string emailAddress, string? messageId = null)
    {
        try
        {
            // For now, just log it. In a full implementation, this would store in an audit table
            _logger.LogInformation("Email sent: User {UserId}, Role {Role}, Type {Type}, Email {Email}, MessageId {MessageId}",
                userId, role, notificationType, emailAddress, messageId);

            // TODO: Implement proper audit table storage
            // var auditRecord = new EmailAudit
            // {
            //     UserId = userId,
            //     Role = role,
            //     NotificationType = notificationType,
            //     EmailAddress = emailAddress,
            //     MessageId = messageId,
            //     Status = "sent",
            //     SentAt = DateTime.UtcNow
            // };
            // _context.EmailAudits.Add(auditRecord);
            // await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record email sent event for user {UserId}", userId);
        }
    }

    public async Task<List<EmailAuditRecord>> GetAuditTrailAsync(Guid userId, DateTime? from = null, DateTime? to = null)
    {
        // TODO: Implement when audit table is available
        // For now, return empty list
        return new List<EmailAuditRecord>();
    }

    private async Task RecordUnsubscribeEventAsync(Guid userId, string role, string notificationType, UnsubscribeScope scope)
    {
        _logger.LogInformation("Unsubscribe event: User {UserId}, Role {Role}, Type {Type}, Scope {Scope}",
            userId, role, notificationType, scope);

        // TODO: Store in audit table for compliance tracking
    }

    private string GenerateComplianceFooter(EmailComplianceRequest request, string unsubscribeUrl)
    {
        return $@"
---
This email was sent by {request.OrganizationName} to {request.RecipientEmail} regarding your {request.Role} account.

You can unsubscribe from this type of email by clicking here: {unsubscribeUrl}

To manage all your notification preferences, visit your account settings.

{request.SenderName}
{request.OrganizationName}
{request.SenderEmail}

This email was sent in compliance with CAN-SPAM and GDPR regulations.
";
    }

    private static string GetUnsubscribeSuccessMessage(UnsubscribeScope scope, int updatedCount)
    {
        return scope switch
        {
            UnsubscribeScope.SpecificNotification => "You have been unsubscribed from this specific notification type.",
            UnsubscribeScope.CategoryNotifications => $"You have been unsubscribed from {updatedCount} related notification types.",
            UnsubscribeScope.AllEmailNotifications => "You have been unsubscribed from all email notifications.",
            _ => "You have been unsubscribed successfully."
        };
    }

    private static string GetNotificationCategory(string notificationType)
    {
        // Map notification types to categories for batch unsubscribe
        var categoryMap = new Dictionary<string, string>
        {
            ["daily_sales_summary"] = "business",
            ["weekly_report"] = "business",
            ["monthly_statement"] = "business",
            ["consignor_signup"] = "consignor",
            ["consignor_item_added"] = "consignor",
            ["pending_approval"] = "consignor",
            ["daily_payout_ready"] = "payout",
            ["weekly_payout_ready"] = "payout",
            ["item_sold"] = "sales",
            ["high_value_sale"] = "sales",
            ["low_inventory"] = "inventory",
            ["pricing_suggestions"] = "inventory",
            ["system_maintenance"] = "system",
            ["security_alerts"] = "system",
            ["account_changes"] = "system",
            ["backup_status"] = "system"
        };

        return categoryMap.GetValueOrDefault(notificationType, "general");
    }

    private static List<string> GetNotificationTypesInCategory(string category)
    {
        var categoryTypes = new Dictionary<string, List<string>>
        {
            ["business"] = new() { "daily_sales_summary", "weekly_report", "monthly_statement" },
            ["consignor"] = new() { "consignor_signup", "consignor_item_added", "pending_approval" },
            ["payout"] = new() { "daily_payout_ready", "weekly_payout_ready" },
            ["sales"] = new() { "item_sold", "high_value_sale" },
            ["inventory"] = new() { "low_inventory", "pricing_suggestions" },
            ["system"] = new() { "system_maintenance", "security_alerts", "account_changes", "backup_status" }
        };

        return categoryTypes.GetValueOrDefault(category, new List<string>());
    }

    private static string EncryptString(string text, string key)
    {
        // Simple encryption for demo - in production use proper encryption library
        var keyBytes = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        using var aes = Aes.Create();
        aes.Key = keyBytes;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var textBytes = Encoding.UTF8.GetBytes(text);
        var encryptedBytes = encryptor.TransformFinalBlock(textBytes, 0, textBytes.Length);

        var result = new byte[aes.IV.Length + encryptedBytes.Length];
        aes.IV.CopyTo(result, 0);
        encryptedBytes.CopyTo(result, aes.IV.Length);

        return Convert.ToBase64String(result);
    }

    private static string DecryptString(string encryptedText, string key)
    {
        var fullBytes = Convert.FromBase64String(encryptedText);
        var keyBytes = SHA256.HashData(Encoding.UTF8.GetBytes(key));

        using var aes = Aes.Create();
        aes.Key = keyBytes;

        var iv = new byte[aes.BlockSize / 8];
        var encryptedBytes = new byte[fullBytes.Length - iv.Length];

        Array.Copy(fullBytes, iv, iv.Length);
        Array.Copy(fullBytes, iv.Length, encryptedBytes, 0, encryptedBytes.Length);

        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        var decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);

        return Encoding.UTF8.GetString(decryptedBytes);
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
}