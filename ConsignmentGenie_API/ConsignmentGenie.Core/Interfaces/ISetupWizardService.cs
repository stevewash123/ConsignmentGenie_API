using ConsignmentGenie.Core.DTOs.SetupWizard;

namespace ConsignmentGenie.Core.Interfaces;

public interface ISetupWizardService
{
    // Wizard Progress
    Task<SetupWizardProgressDto> GetWizardProgressAsync(Guid organizationId);
    Task<SetupWizardStepDto> GetWizardStepAsync(Guid organizationId, int stepNumber);

    // Step 1: Shop Profile
    Task<SetupWizardStepDto> UpdateShopProfileAsync(Guid organizationId, ShopProfileDto shopProfile);

    // Step 2: Business Settings
    Task<SetupWizardStepDto> UpdateBusinessSettingsAsync(Guid organizationId, BusinessSettingsDto businessSettings);

    // Step 3: Storefront Settings
    Task<SetupWizardStepDto> UpdateStorefrontSettingsAsync(Guid organizationId, StorefrontSettingsDto storefrontSettings);

    // Step 4-7: Integrations
    Task<IntegrationStatusDto> GetIntegrationStatusAsync(Guid organizationId);
    Task<SetupWizardStepDto> SetupIntegrationAsync(Guid organizationId, string integrationType, Dictionary<string, string> credentials);

    // Step 8: Complete Setup
    Task<SetupCompleteDto> CompleteSetupAsync(Guid organizationId, bool startTrial = true, string? subscriptionPlan = null);

    // General
    Task<SetupWizardStepDto> MoveToStepAsync(Guid organizationId, int stepNumber);
}