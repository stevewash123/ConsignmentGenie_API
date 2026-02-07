using ConsignmentGenie.Core.Models;

namespace ConsignmentGenie.Core.Interfaces;

/// <summary>
/// Interface for settings page DTOs that support validation
/// </summary>
public interface IPageSettings
{
    /// <summary>
    /// Validate the current settings state
    /// </summary>
    /// <returns>Validation result with success/failure and any errors</returns>
    SettingsValidationResult Validate();
}