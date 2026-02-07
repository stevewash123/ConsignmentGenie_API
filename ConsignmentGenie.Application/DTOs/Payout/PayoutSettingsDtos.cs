namespace ConsignmentGenie.Application.DTOs.Payout;

public class PayoutSettingsDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }

    // === Payout Methods ===
    public bool PayoutMethodCheck { get; set; }
    public bool PayoutMethodCash { get; set; }
    public bool PayoutMethodACH { get; set; }
    public bool PayoutMethodVenmo { get; set; }
    public bool PayoutMethodPayPal { get; set; }
    public bool PayoutMethodStoreCredit { get; set; }
    public bool PayoutMethodZelle { get; set; }

    // === General Settings ===
    public int HoldPeriodDays { get; set; }
    public decimal MinimumPayoutThreshold { get; set; }

    // === Minimum Balance Protection ===
    public decimal MinimumBalanceProtection { get; set; }

    // === Bank Account Connection (for ACH) ===
    public bool BankAccountConnected { get; set; }
    public string PlaidAccessToken { get; set; } = string.Empty;
    public string PlaidAccountId { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string BankAccountLast4 { get; set; } = string.Empty;

    // === Auto-Pay Schedule ===
    public bool AutoPayEnabled { get; set; }
    public bool AutoPayMonday { get; set; }
    public bool AutoPayTuesday { get; set; }
    public bool AutoPayWednesday { get; set; }
    public bool AutoPayThursday { get; set; }
    public bool AutoPayFriday { get; set; }
    public bool AutoPaySaturday { get; set; }
    public bool AutoPaySunday { get; set; }

    // === Additional Settings as JSON ===
    public string ExtendedSettings { get; set; } = "{}";

    // === Audit Fields ===
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreatePayoutSettingsRequest
{
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
    public string ExtendedSettings { get; set; } = "{}";
}

public class UpdatePayoutSettingsRequest
{
    // === Payout Methods ===
    public bool? PayoutMethodCheck { get; set; }
    public bool? PayoutMethodCash { get; set; }
    public bool? PayoutMethodACH { get; set; }
    public bool? PayoutMethodVenmo { get; set; }
    public bool? PayoutMethodPayPal { get; set; }
    public bool? PayoutMethodStoreCredit { get; set; }
    public bool? PayoutMethodZelle { get; set; }

    // === General Settings ===
    public int? HoldPeriodDays { get; set; }
    public decimal? MinimumPayoutThreshold { get; set; }

    // === Minimum Balance Protection ===
    public decimal? MinimumBalanceProtection { get; set; }

    // === Bank Account Connection (for ACH) ===
    public bool? BankAccountConnected { get; set; }
    public string? PlaidAccessToken { get; set; }
    public string? PlaidAccountId { get; set; }
    public string? BankName { get; set; }
    public string? BankAccountLast4 { get; set; }

    // === Auto-Pay Schedule ===
    public bool? AutoPayEnabled { get; set; }
    public bool? AutoPayMonday { get; set; }
    public bool? AutoPayTuesday { get; set; }
    public bool? AutoPayWednesday { get; set; }
    public bool? AutoPayThursday { get; set; }
    public bool? AutoPayFriday { get; set; }
    public bool? AutoPaySaturday { get; set; }
    public bool? AutoPaySunday { get; set; }

    // === Additional Settings as JSON ===
    public string? ExtendedSettings { get; set; }
}

// Simplified request DTO for partial updates matching UI auto-save pattern
public class PartialPayoutSettingsUpdate
{
    [System.Text.Json.Serialization.JsonExtensionData]
    public Dictionary<string, object>? AdditionalProperties { get; set; }
}