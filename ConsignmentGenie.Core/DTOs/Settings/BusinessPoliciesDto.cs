using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ConsignmentGenie.Core.DTOs.Settings;

public class BusinessPoliciesDto
{
    public StoreHoursDto StoreHours { get; set; } = new();
    public ReturnPoliciesDto Returns { get; set; } = new();
    public PaymentPoliciesDto Payments { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

public class StoreHoursDto
{
    public Dictionary<string, DayScheduleDto> Schedule { get; set; } = new();
    public List<SpecialHoursDto>? SpecialHours { get; set; }
    public string Timezone { get; set; } = "EST";
}

public class DayScheduleDto
{
    public bool IsOpen { get; set; }
    public string? OpenTime { get; set; }
    public string? CloseTime { get; set; }
}

public class SpecialHoursDto
{
    public string Date { get; set; } = "";
    public string? Hours { get; set; }
    public bool IsClosed { get; set; }
    public string? Note { get; set; }
}

public class ReturnPoliciesDto
{
    public string ReturnPolicy { get; set; } = "days"; // "none" or "days"

    [Range(1, 365)]
    public int PeriodDays { get; set; } = 30;

    public bool AllowRefunds { get; set; } = true;
    public bool AllowExchanges { get; set; } = true;
    public bool AllowStoreCredit { get; set; } = true;

    public string ReceiptPolicy { get; set; } = "required"; // "required" or "flexible"
    public bool NoReceiptStoreCreditOnly { get; set; } = false;
}

public class PaymentPoliciesDto
{
    public List<string> AcceptedMethods { get; set; } = new();
    public bool LayawayAvailable { get; set; } = false;

    [MaxLength(300)]
    public string? LayawayTerms { get; set; }
}