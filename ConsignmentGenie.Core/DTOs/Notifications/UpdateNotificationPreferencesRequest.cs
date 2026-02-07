namespace ConsignmentGenie.Core.DTOs.Notifications;

public class UpdateNotificationPreferencesRequest
{
    public bool EmailEnabled { get; set; }
    public bool EmailItemSold { get; set; }
    public bool EmailPayoutProcessed { get; set; }
    public bool EmailPayoutPending { get; set; }
    public bool EmailItemExpired { get; set; }
    public bool EmailStatementReady { get; set; }
    public bool EmailAccountUpdate { get; set; }

    public string DigestMode { get; set; } = "instant";
    public string DigestTime { get; set; } = "09:00";
    public int? DigestDay { get; set; }

    public decimal? PayoutPendingThreshold { get; set; }
}