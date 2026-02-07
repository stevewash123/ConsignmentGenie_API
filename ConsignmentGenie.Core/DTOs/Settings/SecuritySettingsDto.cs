namespace ConsignmentGenie.Core.DTOs.Settings;

public class SecuritySettingsDto
{
    public bool HasPin { get; set; }
    public PinRequirementsDto PinRequirements { get; set; } = new();
}

public class PinRequirementsDto
{
    public bool TaxExempt { get; set; }
    public decimal ReturnsOver { get; set; }
    public bool Void { get; set; }
    public bool DrawerOpen { get; set; }
}

public class SetPinRequest
{
    public string Pin { get; set; } = string.Empty;
    public string CurrentPassword { get; set; } = string.Empty;
}

public class ValidatePinRequest
{
    public string Pin { get; set; } = string.Empty;
}

public class ValidatePinResponse
{
    public bool Valid { get; set; }
}

public class UpdatePinRequirementsRequest
{
    public bool TaxExempt { get; set; }
    public decimal ReturnsOver { get; set; }
    public bool Void { get; set; }
    public bool DrawerOpen { get; set; }
}

public class ApprovalResult
{
    public bool Approved { get; set; }
    public string? Reason { get; set; }
}