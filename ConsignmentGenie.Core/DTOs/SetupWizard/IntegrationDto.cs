namespace ConsignmentGenie.Core.DTOs.SetupWizard;

public class IntegrationDto
{
    public string IntegrationType { get; set; } = string.Empty;
    public bool IsConnected { get; set; }
    public bool IsRequired { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ConnectUrl { get; set; }
    public DateTime? LastConnectedAt { get; set; }
    public DateTime? LastErrorAt { get; set; }
    public string? LastErrorMessage { get; set; }
}

public class IntegrationStatusDto
{
    public bool StripeConnected { get; set; }
    public bool QuickBooksConnected { get; set; }
    public bool SendGridConnected { get; set; }
    public bool CloudinaryConnected { get; set; }

    public List<IntegrationDto> Integrations { get; set; } = new();
}

public class IntegrationCredentialsDto
{
    public string IntegrationType { get; set; } = string.Empty;
    public Dictionary<string, string> Credentials { get; set; } = new();
}