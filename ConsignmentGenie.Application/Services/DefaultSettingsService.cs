using ConsignmentGenie.Core.DTOs.Notifications;
using ConsignmentGenie.Core.DTOs.Settings;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Core.Interfaces;
using ConsignmentGenie.Application.Interfaces;
using ConsignmentGenie.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace ConsignmentGenie.Application.Services;

/// <summary>
/// Centralized service for creating default settings for new users
/// Ensures consistency across all user types and setting categories
/// </summary>
public class DefaultSettingsService : IDefaultSettingsService
{
    private readonly INotificationPreferenceService _notificationPreferenceService;
    private readonly IOrganizationSettingsManager _settingsManager;
    private readonly ILogger<DefaultSettingsService> _logger;

    public DefaultSettingsService(
        INotificationPreferenceService notificationPreferenceService,
        IOrganizationSettingsManager settingsManager,
        ILogger<DefaultSettingsService> logger)
    {
        _notificationPreferenceService = notificationPreferenceService;
        _settingsManager = settingsManager;
        _logger = logger;
    }

    /// <summary>
    /// Creates all default settings for a new owner
    /// </summary>
    public async Task CreateOwnerDefaultsAsync(Guid userId, Guid organizationId, string email)
    {
        try
        {
            _logger.LogInformation("[DEFAULT_SETTINGS] Creating owner defaults for user {UserId} ({Email}) in organization {OrganizationId}", userId, email, organizationId);

            // Create notification preferences
            await CreateOwnerNotificationDefaultsAsync(userId, email);

            // Create organization settings defaults
            await CreateOrganizationDefaultsAsync(organizationId);

            _logger.LogInformation("[DEFAULT_SETTINGS] Successfully created owner defaults for {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DEFAULT_SETTINGS] Failed to create owner defaults for {Email}", email);
            throw;
        }
    }

    /// <summary>
    /// Creates all default settings for a new consignor
    /// </summary>
    public async Task CreateConsignorDefaultsAsync(Guid userId, string email)
    {
        try
        {
            _logger.LogInformation("[DEFAULT_SETTINGS] Creating consignor defaults for user {UserId} ({Email})", userId, email);

            // Create notification preferences
            await CreateConsignorNotificationDefaultsAsync(userId, email);

            _logger.LogInformation("[DEFAULT_SETTINGS] Successfully created consignor defaults for {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DEFAULT_SETTINGS] Failed to create consignor defaults for {Email}", email);
            throw;
        }
    }

    /// <summary>
    /// Creates default notification preferences for owners matching UI defaults
    /// </summary>
    private async Task CreateOwnerNotificationDefaultsAsync(Guid userId, string email)
    {
        var defaultPreferences = new NotificationPreferencesMatrix
        {
            UserId = userId,
            Role = "owner",
            PrimaryEmail = email,
            PhoneNumber = null,
            SmsVerified = false,
            LastUpdated = DateTime.UtcNow,
            Preferences = new Dictionary<string, Dictionary<NotificationChannel, bool>>
            {
                // Owner-specific notifications with defaults (email=true, system=true, SMS=false by default)
                ["dropoff_manifest"] = CreateChannelDefaults(email: true, system: true, sms: false),
                ["new_provider_request"] = CreateChannelDefaults(email: true, system: true, sms: false),
                ["low_inventory_alert"] = CreateChannelDefaults(email: true, system: true, sms: false),
                ["daily_sales_summary"] = CreateChannelDefaults(email: true, system: true, sms: false),
                ["suggestion_submitted"] = CreateChannelDefaults(email: true, system: true, sms: false),

                // System notifications (always enabled and locked in UI)
                ["password_reset"] = CreateChannelDefaults(email: true, system: true, sms: false),
                ["account_activated"] = CreateChannelDefaults(email: true, system: true, sms: false),
                ["account_deactivated"] = CreateChannelDefaults(email: true, system: true, sms: false),

                // Payment notifications
                ["payment_received"] = CreateChannelDefaults(email: true, system: true, sms: false),
                ["payment_failed"] = CreateChannelDefaults(email: true, system: true, sms: false),
                ["subscription_expiring"] = CreateChannelDefaults(email: true, system: true, sms: false),

                // Integration notifications
                ["sync_error"] = CreateChannelDefaults(email: true, system: true, sms: false),
                ["sync_completed"] = CreateChannelDefaults(email: false, system: true, sms: false)
            },
            BatchPreferences = new Dictionary<string, string>
            {
                ["daily_sales_summary"] = "daily"
            },
            Thresholds = new Dictionary<string, decimal>
            {
                ["low_inventory_alert"] = 5m,
                ["high_value_sale"] = 100m
            }
        };

        await _notificationPreferenceService.UpdatePreferencesAsync(userId, "owner", defaultPreferences);
        _logger.LogInformation("[DEFAULT_SETTINGS] Created default notification preferences for owner {Email}", email);
    }

    /// <summary>
    /// Creates default notification preferences for consignors
    /// </summary>
    private async Task CreateConsignorNotificationDefaultsAsync(Guid userId, string email)
    {
        var defaultPreferences = new NotificationPreferencesMatrix
        {
            UserId = userId,
            Role = "consignor",
            PrimaryEmail = email,
            PhoneNumber = null,
            SmsVerified = false,
            LastUpdated = DateTime.UtcNow,
            Preferences = new Dictionary<string, Dictionary<NotificationChannel, bool>>
            {
                // Consignor-specific notifications
                ["consignor_approved"] = CreateChannelDefaults(email: true, system: true, sms: false),
                ["consignor_rejected"] = CreateChannelDefaults(email: true, system: true, sms: false),
                ["item_sold"] = CreateChannelDefaults(email: true, system: true, sms: false),
                ["item_rejected"] = CreateChannelDefaults(email: true, system: true, sms: false),
                ["item_activated"] = CreateChannelDefaults(email: true, system: true, sms: false),
                ["payout_ready"] = CreateChannelDefaults(email: true, system: true, sms: false),
                ["payout_processed"] = CreateChannelDefaults(email: true, system: true, sms: false),

                // System notifications
                ["password_reset"] = CreateChannelDefaults(email: true, system: true, sms: false),
                ["welcome"] = CreateChannelDefaults(email: true, system: true, sms: false),
                ["account_activated"] = CreateChannelDefaults(email: true, system: true, sms: false),
                ["account_deactivated"] = CreateChannelDefaults(email: true, system: true, sms: false)
            },
            BatchPreferences = new Dictionary<string, string>(),
            Thresholds = new Dictionary<string, decimal>
            {
                ["high_value_sale"] = 50m // Lower threshold for consignor notifications
            }
        };

        await _notificationPreferenceService.UpdatePreferencesAsync(userId, "consignor", defaultPreferences);
        _logger.LogInformation("[DEFAULT_SETTINGS] Created default notification preferences for consignor {Email}", email);
    }

    /// <summary>
    /// Creates default organization settings for all settings pages
    /// This addresses the Welcome Modal issue by setting proper consignor onboarding defaults
    /// </summary>
    private async Task CreateOrganizationDefaultsAsync(Guid organizationId)
    {
        try
        {
            _logger.LogInformation("[DEFAULT_SETTINGS] Creating organization defaults for {OrganizationId}", organizationId);

            // Create default consignor settings - this fixes the Welcome Modal issue
            await CreateConsignorSettingsDefaultsAsync(organizationId);

            // Create default business settings
            await CreateBusinessSettingsDefaultsAsync(organizationId);

            // Create default storefront settings
            await CreateStorefrontSettingsDefaultsAsync(organizationId);

            _logger.LogInformation("[DEFAULT_SETTINGS] Successfully created organization defaults for {OrganizationId}", organizationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DEFAULT_SETTINGS] Failed to create organization defaults for {OrganizationId}", organizationId);
            throw;
        }
    }

    /// <summary>
    /// Creates default consignor settings - FIXES WELCOME MODAL ISSUE
    /// This method syncs critical fields to Organization table for Welcome Modal logic
    /// </summary>
    private async Task CreateConsignorSettingsDefaultsAsync(Guid organizationId)
    {
        var defaultConsignorSettings = new OrganizationConsignorSettingsDto
        {
            // Default consignor onboarding settings
            // RequireSignedAgreement should be true by default - Welcome Modal shows when true but no agreement uploaded
            RequireSignedAgreement = true,
            AutoSendAgreementOnRegister = false,
            AgreementTemplateId = null,
            AgreementMethod = "upload", // Set to "upload" so Welcome Modal shows (not "none")
            AcknowledgeTermsText = null,

            // Default terms
            ShopCommissionPercent = 50m,
            ConsignmentPeriodDays = 90,
            RetrievalPeriodDays = 30,
            UnsoldItemPolicy = "return",

            // Default permissions
            ApprovalMode = "manual"
        };

        // Save to JSON settings
        await _settingsManager.SetPageSettingsAsync(organizationId, SettingsPage.Consignor, defaultConsignorSettings);

        // **CRITICAL FIX**: Sync key fields to Organization table for Welcome Modal logic
        await _settingsManager.SyncToColumnAsync(organizationId, "AgreementMethod", "AgreementMethod", "upload");
        // Note: AgreementTemplateCloudinaryUrl stays null (no template uploaded yet) - this is correct for Welcome Modal to show

        _logger.LogInformation("[DEFAULT_SETTINGS] Created consignor settings defaults (RequireSignedAgreement=true, AgreementMethod=upload) for organization {OrganizationId}", organizationId);
    }

    /// <summary>
    /// Creates default business settings
    /// </summary>
    private async Task CreateBusinessSettingsDefaultsAsync(Guid organizationId)
    {
        var defaultBusinessSettings = new OrganizationBusinessSettingsDto
        {
            TaxRate = 0m,
            TaxIdEin = null,
            TaxIncludedInPrices = false,
            ChargeTaxOnShipping = false,
            DefaultSplitPercentage = 50m,
            AllowCustomSplitsPerConsignor = true,
            AllowCustomSplitsPerItem = false,
            RefundPolicy = null,
            RefundWindowDays = 30,
            DefaultConsignmentPeriodDays = 90,
            EnableAutoMarkdowns = false,
            ItemSubmissionMode = "manual",
            AutoApproveItems = false
        };

        await _settingsManager.SetPageSettingsAsync(organizationId, SettingsPage.Business, defaultBusinessSettings);
        _logger.LogInformation("[DEFAULT_SETTINGS] Created business settings defaults for organization {OrganizationId}", organizationId);
    }

    /// <summary>
    /// Creates default storefront settings
    /// </summary>
    private async Task CreateStorefrontSettingsDefaultsAsync(Guid organizationId)
    {
        var defaultStorefrontSettings = new StorefrontSettingsDto
        {
            SelectedChannel = "cg-storefront",
            Square = new SquareSettingsDto
            {
                Connected = false,
                SyncInventory = true,
                ImportSales = false,
                CategoryMappings = new List<CategoryMappingDto>()
            },
            Shopify = new ShopifySettingsDto
            {
                Connected = false,
                PushInventory = true,
                ImportOrders = true,
                SyncImages = true,
                AutoMarkSold = true,
                CollectionMappings = new List<CollectionMappingDto>()
            },
            CgStorefront = new CgStorefrontSettingsDto
            {
                StoreSlug = "",
                CustomDomain = null,
                DnsVerified = false,
                StripeConnected = false,
                StripeAccountName = null,
                BannerImageUrl = null,
                PrimaryColor = "#2563eb",
                AccentColor = "#1d4ed8",
                MetaTitle = null,
                MetaDescription = null,
                PaymentSettings = new StorefrontPaymentSettingsDto
                {
                    EnableCreditCards = true,
                    EnableBuyNow = true,
                    EnableLayaway = false,
                    LayawayDepositPercentage = 25,
                    LayawayTermsInDays = 30
                },
                ShippingSettings = new StorefrontShippingSettingsDto
                {
                    EnableShipping = false,
                    FlatRate = 0m,
                    FreeShippingThreshold = 0m,
                    ShipsFromZipCode = ""
                },
                SalesSettings = new StorefrontSalesSettingsDto
                {
                    EnableBestOffer = false,
                    AutoAcceptPercentage = 0,
                    MinimumOfferPercentage = 0
                }
            },
            InStore = new InStoreSettingsDto
            {
                UseReceiptNumbers = true,
                ReceiptPrefix = null,
                NextReceiptNumber = 1,
                RequireManagerApproval = false,
                AllowLayaway = false
            }
        };

        await _settingsManager.SetPageSettingsAsync(organizationId, SettingsPage.Storefront, defaultStorefrontSettings);
        _logger.LogInformation("[DEFAULT_SETTINGS] Created storefront settings defaults for organization {OrganizationId}", organizationId);
    }

    /// <summary>
    /// Helper method to create channel preference dictionaries
    /// </summary>
    private static Dictionary<NotificationChannel, bool> CreateChannelDefaults(bool email = false, bool system = true, bool sms = false)
    {
        return new Dictionary<NotificationChannel, bool>
        {
            [NotificationChannel.Email] = email,
            [NotificationChannel.System] = system,
            [NotificationChannel.Sms] = sms
        };
    }
}