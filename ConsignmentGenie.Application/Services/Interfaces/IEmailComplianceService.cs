using ConsignmentGenie.Application.Services.Interfaces;

namespace ConsignmentGenie.Application.Services.Interfaces;

/// <summary>
/// Service for email compliance (CAN-SPAM, GDPR) with unsubscribe functionality
/// linked to the notification preference matrix
/// </summary>
public interface IEmailComplianceService
{
    /// <summary>
    /// Generates a secure unsubscribe token for a user and specific notification type
    /// </summary>
    Task<string> GenerateUnsubscribeTokenAsync(Guid userId, string role, string notificationType);

    /// <summary>
    /// Validates an unsubscribe token and extracts user/notification information
    /// </summary>
    Task<UnsubscribeTokenInfo?> ValidateUnsubscribeTokenAsync(string token);

    /// <summary>
    /// Processes an unsubscribe request, updating the notification preference matrix
    /// </summary>
    Task<UnsubscribeResult> ProcessUnsubscribeAsync(string token, UnsubscribeScope scope = UnsubscribeScope.SpecificNotification);

    /// <summary>
    /// Gets unsubscribe URL for embedding in emails
    /// </summary>
    string GetUnsubscribeUrl(string baseUrl, string token);

    /// <summary>
    /// Adds required compliance headers and footer to email content
    /// </summary>
    Task<EmailComplianceResult> AddComplianceContentAsync(EmailComplianceRequest request);

    /// <summary>
    /// Validates that an email can be sent according to preference matrix and compliance rules
    /// </summary>
    Task<bool> CanSendEmailAsync(Guid userId, string role, string notificationType);

    /// <summary>
    /// Records email send event for audit and compliance tracking
    /// </summary>
    Task RecordEmailSentAsync(Guid userId, string role, string notificationType, string emailAddress, string? messageId = null);

    /// <summary>
    /// Gets compliance audit trail for a user
    /// </summary>
    Task<List<EmailAuditRecord>> GetAuditTrailAsync(Guid userId, DateTime? from = null, DateTime? to = null);
}

/// <summary>
/// Information extracted from an unsubscribe token
/// </summary>
public class UnsubscribeTokenInfo
{
    public Guid UserId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string NotificationType { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
}

/// <summary>
/// Result of processing an unsubscribe request
/// </summary>
public class UnsubscribeResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public UnsubscribeScope ProcessedScope { get; set; }
    public List<string> UpdatedNotificationTypes { get; set; } = new();
}

/// <summary>
/// Scope of unsubscribe operation
/// </summary>
public enum UnsubscribeScope
{
    SpecificNotification,  // Only the specific notification type
    CategoryNotifications, // All notifications in the same category (e.g., all sales notifications)
    AllEmailNotifications // All email notifications for the user
}

/// <summary>
/// Request for adding compliance content to emails
/// </summary>
public class EmailComplianceRequest
{
    public string EmailContent { get; set; } = string.Empty;
    public string EmailSubject { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string NotificationType { get; set; } = string.Empty;
    public string RecipientEmail { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
}

/// <summary>
/// Result of adding compliance content
/// </summary>
public class EmailComplianceResult
{
    public string EmailContent { get; set; } = string.Empty;
    public string EmailSubject { get; set; } = string.Empty;
    public string UnsubscribeToken { get; set; } = string.Empty;
    public string UnsubscribeUrl { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = new();
}

/// <summary>
/// Email audit record for compliance tracking
/// </summary>
public class EmailAuditRecord
{
    public DateTime SentAt { get; set; }
    public Guid UserId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string NotificationType { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
    public string? MessageId { get; set; }
    public string Status { get; set; } = string.Empty; // sent, delivered, bounced, complained
}