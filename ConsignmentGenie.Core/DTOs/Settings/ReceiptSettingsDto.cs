using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.DTOs.Settings;

public class ReceiptSettingsDto
{
    public ReceiptHeaderDto Header { get; set; } = new();
    public ReceiptContentDto Content { get; set; } = new();
    public ReceiptFooterDto Footer { get; set; } = new();
    public DigitalReceiptDto Digital { get; set; } = new();
    public PrintReceiptDto Print { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

public class ReceiptHeaderDto
{
    public bool IncludeLogo { get; set; } = false;
    public bool ShowStoreInfo { get; set; } = true;
    public bool ShowAddress { get; set; } = true;
    public string DateFormat { get; set; } = "MM/dd/yyyy"; // "MM/dd/yyyy", "dd/MM/yyyy", "yyyy-MM-dd"
    public string TimeFormat { get; set; } = "12h"; // "12h" or "24h"
}

public class ReceiptContentDto
{
    public string LayoutStyle { get; set; } = "simple"; // "compact" or "detailed"
    public bool ShowItemDescriptions { get; set; } = true;
    public int DescriptionLength { get; set; } = 50;
    public bool ShowTaxBreakdown { get; set; } = true;
    public bool ShowPaymentMethod { get; set; } = true;
}

public class ReceiptFooterDto
{
    [MaxLength(200)]
    public string? CustomMessage { get; set; }

    public bool IncludeReturnPolicy { get; set; } = false;

    [MaxLength(300)]
    public string? ReturnPolicyText { get; set; }

    public string? ThankYouMessage { get; set; }
    public bool IncludeWebsite { get; set; } = false;
}

public class DigitalReceiptDto
{
    public bool AutoEmailReceipts { get; set; } = false;
    public string EmailFormat { get; set; } = "html"; // "html" or "pdf"
    public bool PromptForEmail { get; set; } = false;
    public string EmailSubject { get; set; } = "Your receipt from [Store Name]";
    public bool IncludePromoContent { get; set; } = false;

    [MaxLength(250)]
    public string? PromoContent { get; set; }
}

public class PrintReceiptDto
{
    public bool AutoPrint { get; set; } = true;
    public int PrinterWidth { get; set; } = 80; // 58 or 80
    public int Copies { get; set; } = 1; // 1 or 2
    public string PrintDensity { get; set; } = "medium"; // "light", "medium", "dark"
    public string LogoSize { get; set; } = "medium"; // "small", "medium", "large"
}