namespace ConsignmentGenie.Core.Models;

/// <summary>
/// Represents the result of a validation operation for settings
/// </summary>
public class SettingsValidationResult
{
    public bool IsValid { get; private set; }
    public string Message { get; private set; }
    public List<string> Errors { get; private set; }

    private SettingsValidationResult(bool isValid, string message, List<string>? errors = null)
    {
        IsValid = isValid;
        Message = message;
        Errors = errors ?? new List<string>();
    }

    /// <summary>
    /// Create a successful validation result
    /// </summary>
    public static SettingsValidationResult Success(string message = "Validation passed") =>
        new(true, message);

    /// <summary>
    /// Create a failed validation result with a single error
    /// </summary>
    public static SettingsValidationResult Error(string message) =>
        new(false, message, new List<string> { message });

    /// <summary>
    /// Create a failed validation result with multiple errors
    /// </summary>
    public static SettingsValidationResult WithErrors(IEnumerable<string> errors)
    {
        var errorList = errors.ToList();
        return new(false, $"Validation failed with {errorList.Count} error(s)", errorList);
    }

    /// <summary>
    /// Combine multiple validation results
    /// </summary>
    public static SettingsValidationResult Combine(params SettingsValidationResult[] results)
    {
        var allErrors = results.Where(r => !r.IsValid).SelectMany(r => r.Errors).ToList();

        if (allErrors.Count == 0)
            return Success();

        return WithErrors(allErrors);
    }
}