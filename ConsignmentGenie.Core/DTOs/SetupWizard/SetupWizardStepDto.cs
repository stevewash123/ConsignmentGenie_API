namespace ConsignmentGenie.Core.DTOs.SetupWizard;

public class SetupWizardStepDto
{
    public int StepNumber { get; set; }
    public string StepTitle { get; set; } = string.Empty;
    public string StepDescription { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public bool IsCurrentStep { get; set; }
    public object? StepData { get; set; }
}

public class SetupWizardProgressDto
{
    public int CurrentStep { get; set; }
    public int TotalSteps { get; set; }
    public double ProgressPercentage { get; set; }
    public List<SetupWizardStepDto> Steps { get; set; } = new();
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
}