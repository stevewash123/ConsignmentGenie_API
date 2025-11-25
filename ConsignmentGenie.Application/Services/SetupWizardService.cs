using ConsignmentGenie.Core.DTOs.SetupWizard;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Interfaces;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ConsignmentGenie.Application.Services;

public class SetupWizardService : ISetupWizardService
{
    private readonly ConsignmentGenieContext _context;
    private readonly ILogger<SetupWizardService> _logger;

    private readonly List<(int StepNumber, string Title, string Description)> _wizardSteps = new()
    {
        (1, "Shop Profile", "Basic information about your shop"),
        (2, "Business Settings", "Commission rates and tax settings"),
        (3, "Storefront Settings", "Online store and fulfillment options"),
        (4, "Stripe Integration", "Payment processing setup"),
        (5, "QuickBooks Integration", "Accounting software connection"),
        (6, "Email Integration", "Email notification setup"),
        (7, "Image Storage", "Photo management integration"),
        (8, "Complete Setup", "Review and activate your shop")
    };

    public SetupWizardService(ConsignmentGenieContext context, ILogger<SetupWizardService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<SetupWizardProgressDto> GetWizardProgressAsync(Guid organizationId)
    {
        var organization = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Id == organizationId);

        if (organization == null)
            throw new ArgumentException("Organization not found", nameof(organizationId));

        var currentStep = organization.SetupStep;
        var isCompleted = organization.SetupCompletedAt.HasValue;

        var steps = _wizardSteps.Select(s => new SetupWizardStepDto
        {
            StepNumber = s.StepNumber,
            StepTitle = s.Title,
            StepDescription = s.Description,
            IsCompleted = currentStep > s.StepNumber || isCompleted,
            IsCurrentStep = currentStep == s.StepNumber && !isCompleted
        }).ToList();

        return new SetupWizardProgressDto
        {
            CurrentStep = currentStep,
            TotalSteps = _wizardSteps.Count,
            ProgressPercentage = isCompleted ? 100.0 : (double)currentStep / _wizardSteps.Count * 100.0,
            Steps = steps,
            IsCompleted = isCompleted,
            CompletedAt = organization.SetupCompletedAt
        };
    }

    public async Task<SetupWizardStepDto> GetWizardStepAsync(Guid organizationId, int stepNumber)
    {
        var organization = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Id == organizationId);

        if (organization == null)
            throw new ArgumentException("Organization not found", nameof(organizationId));

        var stepInfo = _wizardSteps.FirstOrDefault(s => s.StepNumber == stepNumber);
        if (stepInfo == default)
            throw new ArgumentException("Invalid step number", nameof(stepNumber));

        var stepData = stepNumber switch
        {
            1 => await GetShopProfileDataAsync(organization),
            2 => await GetBusinessSettingsDataAsync(organization),
            3 => await GetStorefrontSettingsDataAsync(organization),
            4 or 5 or 6 or 7 => await GetIntegrationDataAsync(organization, stepNumber),
            8 => await GetCompleteSetupDataAsync(organization),
            _ => null
        };

        return new SetupWizardStepDto
        {
            StepNumber = stepNumber,
            StepTitle = stepInfo.Title,
            StepDescription = stepInfo.Description,
            IsCompleted = organization.SetupStep > stepNumber || organization.SetupCompletedAt.HasValue,
            IsCurrentStep = organization.SetupStep == stepNumber,
            StepData = stepData
        };
    }

    public async Task<SetupWizardStepDto> UpdateShopProfileAsync(Guid organizationId, ShopProfileDto shopProfile)
    {
        var organization = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Id == organizationId);

        if (organization == null)
            throw new ArgumentException("Organization not found", nameof(organizationId));

        // Update shop profile
        organization.ShopName = shopProfile.ShopName;
        organization.ShopDescription = shopProfile.ShopDescription;
        organization.ShopLogoUrl = shopProfile.ShopLogoUrl;
        organization.ShopBannerUrl = shopProfile.ShopBannerUrl;
        organization.ShopEmail = shopProfile.ShopEmail;
        organization.ShopPhone = shopProfile.ShopPhone;
        organization.ShopWebsite = shopProfile.ShopWebsite;
        organization.ShopAddress1 = shopProfile.ShopAddress1;
        organization.ShopAddress2 = shopProfile.ShopAddress2;
        organization.ShopCity = shopProfile.ShopCity;
        organization.ShopState = shopProfile.ShopState;
        organization.ShopZip = shopProfile.ShopZip;
        organization.ShopCountry = shopProfile.ShopCountry;
        organization.ShopTimezone = shopProfile.ShopTimezone;
        organization.UpdatedAt = DateTime.UtcNow;

        // Advance to step 2 if we're on step 1
        if (organization.SetupStep < 2)
        {
            organization.SetupStep = 2;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Shop profile updated for organization {OrganizationId}", organizationId);

        return await GetWizardStepAsync(organizationId, 2);
    }

    public async Task<SetupWizardStepDto> UpdateBusinessSettingsAsync(Guid organizationId, BusinessSettingsDto businessSettings)
    {
        var organization = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Id == organizationId);

        if (organization == null)
            throw new ArgumentException("Organization not found", nameof(organizationId));

        // Update business settings
        organization.DefaultSplitPercentage = businessSettings.DefaultSplitPercentage;
        organization.TaxRate = businessSettings.TaxRate;
        organization.Currency = businessSettings.Currency;
        organization.UpdatedAt = DateTime.UtcNow;

        // Advance to step 3 if we're on step 2
        if (organization.SetupStep < 3)
        {
            organization.SetupStep = 3;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Business settings updated for organization {OrganizationId}", organizationId);

        return await GetWizardStepAsync(organizationId, 3);
    }

    public async Task<SetupWizardStepDto> UpdateStorefrontSettingsAsync(Guid organizationId, StorefrontSettingsDto storefrontSettings)
    {
        var organization = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Id == organizationId);

        if (organization == null)
            throw new ArgumentException("Organization not found", nameof(organizationId));

        // Update storefront settings
        organization.Slug = storefrontSettings.Slug;
        organization.StoreEnabled = storefrontSettings.StoreEnabled;
        organization.ShippingEnabled = storefrontSettings.ShippingEnabled;
        organization.ShippingFlatRate = storefrontSettings.ShippingFlatRate;
        organization.PickupEnabled = storefrontSettings.PickupEnabled;
        organization.PickupInstructions = storefrontSettings.PickupInstructions;
        organization.PayOnPickupEnabled = storefrontSettings.PayOnPickupEnabled;
        organization.OnlinePaymentEnabled = storefrontSettings.OnlinePaymentEnabled;
        organization.UpdatedAt = DateTime.UtcNow;

        // Advance to step 4 if we're on step 3
        if (organization.SetupStep < 4)
        {
            organization.SetupStep = 4;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Storefront settings updated for organization {OrganizationId}", organizationId);

        return await GetWizardStepAsync(organizationId, 4);
    }

    public async Task<IntegrationStatusDto> GetIntegrationStatusAsync(Guid organizationId)
    {
        var organization = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Id == organizationId);

        if (organization == null)
            throw new ArgumentException("Organization not found", nameof(organizationId));

        var integrations = new List<IntegrationDto>
        {
            new()
            {
                IntegrationType = "stripe",
                DisplayName = "Stripe",
                Description = "Payment processing for online orders",
                IsConnected = organization.StripeConnected,
                IsRequired = organization.OnlinePaymentEnabled
            },
            new()
            {
                IntegrationType = "quickbooks",
                DisplayName = "QuickBooks",
                Description = "Accounting and bookkeeping integration",
                IsConnected = organization.QuickBooksConnected,
                IsRequired = false
            },
            new()
            {
                IntegrationType = "sendgrid",
                DisplayName = "SendGrid",
                Description = "Email notifications and marketing",
                IsConnected = organization.SendGridConnected,
                IsRequired = false
            },
            new()
            {
                IntegrationType = "cloudinary",
                DisplayName = "Cloudinary",
                Description = "Image storage and optimization",
                IsConnected = organization.CloudinaryConnected,
                IsRequired = false
            }
        };

        return new IntegrationStatusDto
        {
            StripeConnected = organization.StripeConnected,
            QuickBooksConnected = organization.QuickBooksConnected,
            SendGridConnected = organization.SendGridConnected,
            CloudinaryConnected = organization.CloudinaryConnected,
            Integrations = integrations
        };
    }

    public async Task<SetupWizardStepDto> SetupIntegrationAsync(Guid organizationId, string integrationType, Dictionary<string, string> credentials)
    {
        var organization = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Id == organizationId);

        if (organization == null)
            throw new ArgumentException("Organization not found", nameof(organizationId));

        // TODO: Implement actual integration setup logic
        // For now, just mark as connected
        switch (integrationType.ToLower())
        {
            case "stripe":
                organization.StripeConnected = true;
                break;
            case "quickbooks":
                organization.QuickBooksConnected = true;
                break;
            case "sendgrid":
                organization.SendGridConnected = true;
                break;
            case "cloudinary":
                organization.CloudinaryConnected = true;
                break;
            default:
                throw new ArgumentException("Unknown integration type", nameof(integrationType));
        }

        organization.UpdatedAt = DateTime.UtcNow;

        // Advance setup step if needed
        var currentStep = organization.SetupStep;
        var newStep = integrationType.ToLower() switch
        {
            "stripe" when currentStep == 4 => 5,
            "quickbooks" when currentStep == 5 => 6,
            "sendgrid" when currentStep == 6 => 7,
            "cloudinary" when currentStep == 7 => 8,
            _ => currentStep
        };

        if (newStep > currentStep)
        {
            organization.SetupStep = newStep;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Integration {IntegrationType} setup for organization {OrganizationId}", integrationType, organizationId);

        return await GetWizardStepAsync(organizationId, newStep);
    }

    public async Task<SetupCompleteDto> CompleteSetupAsync(Guid organizationId, bool startTrial = true, string? subscriptionPlan = null)
    {
        var organization = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Id == organizationId);

        if (organization == null)
            throw new ArgumentException("Organization not found", nameof(organizationId));

        // Mark setup as complete
        organization.SetupCompletedAt = DateTime.UtcNow;
        organization.SetupStep = 8;

        // Start trial if requested
        if (startTrial && organization.Status == "pending")
        {
            organization.Status = "trial";
            organization.TrialStartedAt = DateTime.UtcNow;
            organization.TrialEndsAt = DateTime.UtcNow.AddDays(14); // 14-day trial
        }

        if (!string.IsNullOrEmpty(subscriptionPlan))
        {
            organization.SubscriptionPlan = subscriptionPlan;
        }

        organization.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Setup completed for organization {OrganizationId}", organizationId);

        var trialInfo = new TrialSubscriptionDto
        {
            Status = organization.Status,
            TrialStartedAt = organization.TrialStartedAt,
            TrialEndsAt = organization.TrialEndsAt,
            DaysRemaining = organization.TrialEndsAt?.Subtract(DateTime.UtcNow).Days ?? 0,
            TrialExtensionsUsed = organization.TrialExtensionsUsed,
            CanExtendTrial = organization.TrialExtensionsUsed < 2,
            SubscriptionTier = organization.SubscriptionTier,
            SubscriptionPlan = organization.SubscriptionPlan,
            StripeSubscriptionStatus = organization.StripeSubscriptionStatus,
            SubscriptionStartedAt = organization.SubscriptionStartedAt,
            CurrentPeriodEnd = organization.CurrentPeriodEnd
        };

        var integrationStatus = await GetIntegrationStatusAsync(organizationId);

        return new SetupCompleteDto
        {
            OrganizationName = organization.Name,
            ShopName = organization.ShopName ?? organization.Name,
            Slug = organization.Slug,
            StoreEnabled = organization.StoreEnabled,
            CompletedAt = organization.SetupCompletedAt.Value,
            TrialInfo = trialInfo,
            IntegrationsStatus = integrationStatus
        };
    }

    public async Task<SetupWizardStepDto> MoveToStepAsync(Guid organizationId, int stepNumber)
    {
        var organization = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Id == organizationId);

        if (organization == null)
            throw new ArgumentException("Organization not found", nameof(organizationId));

        if (stepNumber < 1 || stepNumber > _wizardSteps.Count)
            throw new ArgumentException("Invalid step number", nameof(stepNumber));

        organization.SetupStep = stepNumber;
        organization.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetWizardStepAsync(organizationId, stepNumber);
    }

    // Private helper methods
    private async Task<object> GetShopProfileDataAsync(Organization organization)
    {
        return new ShopProfileDto
        {
            ShopName = organization.ShopName ?? string.Empty,
            ShopDescription = organization.ShopDescription,
            ShopLogoUrl = organization.ShopLogoUrl,
            ShopBannerUrl = organization.ShopBannerUrl,
            ShopEmail = organization.ShopEmail ?? string.Empty,
            ShopPhone = organization.ShopPhone,
            ShopWebsite = organization.ShopWebsite,
            ShopAddress1 = organization.ShopAddress1,
            ShopAddress2 = organization.ShopAddress2,
            ShopCity = organization.ShopCity,
            ShopState = organization.ShopState,
            ShopZip = organization.ShopZip,
            ShopCountry = organization.ShopCountry,
            ShopTimezone = organization.ShopTimezone
        };
    }

    private async Task<object> GetBusinessSettingsDataAsync(Organization organization)
    {
        return new BusinessSettingsDto
        {
            DefaultSplitPercentage = organization.DefaultSplitPercentage,
            TaxRate = organization.TaxRate,
            Currency = organization.Currency
        };
    }

    private async Task<object> GetStorefrontSettingsDataAsync(Organization organization)
    {
        return new StorefrontSettingsDto
        {
            Slug = organization.Slug,
            StoreEnabled = organization.StoreEnabled,
            ShippingEnabled = organization.ShippingEnabled,
            ShippingFlatRate = organization.ShippingFlatRate,
            PickupEnabled = organization.PickupEnabled,
            PickupInstructions = organization.PickupInstructions,
            PayOnPickupEnabled = organization.PayOnPickupEnabled,
            OnlinePaymentEnabled = organization.OnlinePaymentEnabled
        };
    }

    private async Task<object> GetIntegrationDataAsync(Organization organization, int stepNumber)
    {
        return await GetIntegrationStatusAsync(organization.Id);
    }

    private async Task<object> GetCompleteSetupDataAsync(Organization organization)
    {
        var integrationStatus = await GetIntegrationStatusAsync(organization.Id);
        return new
        {
            ReadyToComplete = true,
            IntegrationStatus = integrationStatus,
            TrialInfo = new
            {
                TrialLength = 14,
                CanStartTrial = organization.Status == "pending"
            }
        };
    }
}