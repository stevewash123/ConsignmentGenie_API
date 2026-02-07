using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace ConsignmentGenie.Core.Entities;

public class PayoutSettings
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid OrganizationId { get; set; }

    [ForeignKey(nameof(OrganizationId))]
    public Organization Organization { get; set; } = null!;

    // === Payout Methods ===
    public bool PayoutMethodCheck { get; set; } = true;
    public bool PayoutMethodCash { get; set; } = true;
    public bool PayoutMethodACH { get; set; } = false;
    public bool PayoutMethodVenmo { get; set; } = false;
    public bool PayoutMethodPayPal { get; set; } = false;
    public bool PayoutMethodStoreCredit { get; set; } = false;
    public bool PayoutMethodZelle { get; set; } = false;

    // === General Settings ===
    public int HoldPeriodDays { get; set; } = 7;
    public decimal MinimumPayoutThreshold { get; set; } = 25.00m;

    // === Minimum Balance Protection ===
    public decimal MinimumBalanceProtection { get; set; } = 100.00m;

    // === Bank Account Connection (for ACH) ===
    public bool BankAccountConnected { get; set; } = false;
    public string PlaidAccessToken { get; set; } = string.Empty;
    public string PlaidAccountId { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string BankAccountLast4 { get; set; } = string.Empty;

    // === Dwolla Integration ===
    public string DwollaCustomerId { get; set; } = string.Empty;
    public string DwollaCustomerUrl { get; set; } = string.Empty;
    public string DwollaCustomerStatus { get; set; } = string.Empty;

    // === Auto-Pay Schedule ===
    public bool AutoPayEnabled { get; set; } = false;
    public bool AutoPayMonday { get; set; } = false;
    public bool AutoPayTuesday { get; set; } = false;
    public bool AutoPayWednesday { get; set; } = false;
    public bool AutoPayThursday { get; set; } = false;
    public bool AutoPayFriday { get; set; } = false;
    public bool AutoPaySaturday { get; set; } = false;
    public bool AutoPaySunday { get; set; } = false;

    // === Additional Settings as JSON ===
    [Column(TypeName = "jsonb")]
    public string ExtendedSettings { get; set; } = "{}";

    // === Audit Fields ===
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Helper method to get/set extended settings
    public T? GetExtendedSetting<T>(string key)
    {
        try
        {
            var jsonDoc = JsonDocument.Parse(ExtendedSettings);
            if (jsonDoc.RootElement.TryGetProperty(key, out var property))
            {
                return JsonSerializer.Deserialize<T>(property.GetRawText());
            }
        }
        catch (JsonException)
        {
            // Return default if JSON is invalid
        }
        return default(T);
    }

    public void SetExtendedSetting<T>(string key, T value)
    {
        try
        {
            var jsonDoc = JsonDocument.Parse(ExtendedSettings);
            var existingDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(ExtendedSettings)
                ?? new Dictionary<string, JsonElement>();

            existingDict[key] = JsonSerializer.SerializeToElement(value);
            ExtendedSettings = JsonSerializer.Serialize(existingDict);
        }
        catch (JsonException)
        {
            // Create new JSON if current is invalid
            var newDict = new Dictionary<string, JsonElement> { [key] = JsonSerializer.SerializeToElement(value) };
            ExtendedSettings = JsonSerializer.Serialize(newDict);
        }
    }
}