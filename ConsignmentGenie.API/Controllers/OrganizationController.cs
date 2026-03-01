using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ConsignmentGenie.Core.DTOs.Onboarding;
using ConsignmentGenie.Core.DTOs.Organization;
using ConsignmentGenie.Core.DTOs.Settings;
using ConsignmentGenie.Core.Enums;
using System.Text.Json;
using ConsignmentGenie.Application.Services;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/organizations")]
[Authorize(Roles = "Owner")]
public class OrganizationController : ControllerBase
{
    private readonly ConsignmentGenieContext _context;
    private readonly ILogger<OrganizationController> _logger;
    private readonly CloudinaryPhotoService _cloudinaryPhotoService;

    public OrganizationController(ConsignmentGenieContext context, ILogger<OrganizationController> logger, CloudinaryPhotoService cloudinaryPhotoService)
    {
        _context = context;
        _logger = logger;
        _cloudinaryPhotoService = cloudinaryPhotoService;
    }

    [HttpGet("setup-status")]
    public async Task<ActionResult<object>> GetSetupStatus()
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[SETUP] Getting setup status for organization {OrganizationId}", organizationId);

        try
        {
            var organization = await _context.Organizations
                .Include(o => o.Consignors)
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                _logger.LogWarning("[SETUP] Organization {OrganizationId} not found", organizationId);
                return NotFound("Organization not found");
            }

            _logger.LogDebug("[SETUP] Organization {OrganizationId} found: Name={OrganizationName}, WelcomeGuideCompleted={WelcomeGuideCompleted}, ProviderCount={ProviderCount}, ItemCount={ItemCount}, StoreEnabled={StoreEnabled}, StripeConnected={StripeConnected}, QuickBooksConnected={QuickBooksConnected}",
                organizationId, organization.Name, organization.WelcomeGuideCompleted, organization.Consignors?.Count ?? 0, organization.Items?.Count ?? 0, organization.StoreEnabled, organization.StripeConnected, organization.QuickBooksConnected);

            var hasProviders = organization.Consignors.Any();
            var storefrontConfigured = organization.StoreEnabled ||
                                      organization.StripeConnected;
            var hasInventory = organization.Items.Any();
            var quickBooksConnected = organization.QuickBooksConnected;

            // Calculate showModal based on specification logic
            var showModal = !organization.WelcomeGuideCompleted && (
                !hasProviders ||
                !storefrontConfigured ||
                !hasInventory ||
                !quickBooksConnected
            );

            var status = new OnboardingStatusDto
            {
                Dismissed = organization.OnboardingDismissed,
                WelcomeGuideCompleted = organization.WelcomeGuideCompleted,
                ShowModal = showModal,
                Steps = new OnboardingStepsDto
                {
                    HasProviders = hasProviders,
                    StorefrontConfigured = storefrontConfigured,
                    HasInventory = hasInventory,
                    QuickBooksConnected = quickBooksConnected
                }
            };

            _logger.LogInformation("[SETUP] Setup status calculated for organization {OrganizationId}: WelcomeGuideCompleted={WelcomeGuideCompleted}, ShowModal={ShowModal}, HasProviders={HasProviders}, StorefrontConfigured={StorefrontConfigured}, HasInventory={HasInventory}, QuickBooksConnected={QuickBooksConnected}",
                organizationId, status.WelcomeGuideCompleted, showModal, hasProviders, storefrontConfigured, hasInventory, quickBooksConnected);

            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SETUP] Error getting setup status for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("dismiss-welcome-guide")]
    public async Task<ActionResult<object>> DismissWelcomeGuide()
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[SETUP] Dismissing welcome guide for organization {OrganizationId}", organizationId);

        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                _logger.LogWarning("[SETUP] Organization {OrganizationId} not found during dismiss operation", organizationId);
                return NotFound("Organization not found");
            }

            var previousStatus = organization.WelcomeGuideCompleted;
            organization.WelcomeGuideCompleted = true;
            organization.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("[SETUP] Welcome guide dismissed for organization {OrganizationId}: {PreviousStatus} -> {NewStatus}",
                organizationId, previousStatus, true);

            return Ok(new {
                success = true,
                welcomeGuideCompleted = true,
                message = "Welcome guide dismissed successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SETUP] Error dismissing welcome guide for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }


    [HttpGet("business-settings")]
    public async Task<ActionResult<BusinessSettingsDto>> GetBusinessSettings()
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[BUSINESS_SETTINGS] Getting business settings for organization {OrganizationId}", organizationId);

        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                _logger.LogWarning("[BUSINESS_SETTINGS] Organization {OrganizationId} not found", organizationId);
                return NotFound("Organization not found");
            }

            // Parse JSON settings or create defaults
            BusinessSettingsDto? businessSettings = null;
            if (!string.IsNullOrEmpty(organization.BusinessSettings))
            {
                try
                {
                    businessSettings = JsonSerializer.Deserialize<BusinessSettingsDto>(organization.BusinessSettings);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "[BUSINESS_SETTINGS] Failed to parse business settings JSON for organization {OrganizationId}", organizationId);
                }
            }

            // Create defaults if parsing failed or no settings exist
            businessSettings ??= new BusinessSettingsDto
            {
                Commission = new CommissionDto
                {
                    DefaultSplit = organization.DefaultSplitPercentage == 60 ? "60/40" : "70/30",
                    AllowCustomSplitsPerConsignor = false,
                    AllowCustomSplitsPerItem = false
                },
                Tax = new TaxDto
                {
                    SalesTaxRate = organization.TaxRate * 100, // Convert from decimal to percentage
                    TaxIncludedInPrices = false,
                    ChargeTaxOnShipping = false
                },
                Payouts = new PayoutDto
                {
                    Schedule = "monthly",
                    MinimumAmount = 25.00m,
                    HoldPeriodDays = 14
                },
                Items = new ItemPolicyDto
                {
                    DefaultConsignmentPeriodDays = 90,
                    EnableAutoMarkdowns = false,
                    MarkdownSchedule = new MarkdownScheduleDto
                    {
                        After30Days = 0,
                        After60Days = 0,
                        After90DaysAction = "return"
                    }
                }
            };

            _logger.LogDebug("[BUSINESS_SETTINGS] Business settings retrieved for organization {OrganizationId}", organizationId);
            return Ok(businessSettings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BUSINESS_SETTINGS] Error getting business settings for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("business-settings")]
    public async Task<ActionResult<object>> UpdateBusinessSettings([FromBody] BusinessSettingsDto businessSettingsDto)
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[BUSINESS_SETTINGS] Updating business settings for organization {OrganizationId}", organizationId);

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                _logger.LogWarning("[BUSINESS_SETTINGS] Organization {OrganizationId} not found during update", organizationId);
                return NotFound("Organization not found");
            }

            // Update basic fields that exist in Organization entity
            var splitParts = businessSettingsDto.Commission.DefaultSplit.Split('/');
            if (splitParts.Length == 2 && decimal.TryParse(splitParts[0], out var consignorPercentage))
            {
                organization.DefaultSplitPercentage = consignorPercentage;
            }

            organization.TaxRate = businessSettingsDto.Tax.SalesTaxRate / 100; // Convert from percentage to decimal

            // Store full settings as JSON
            organization.BusinessSettings = JsonSerializer.Serialize(businessSettingsDto);
            organization.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("[BUSINESS_SETTINGS] Business settings updated for organization {OrganizationId}", organizationId);

            return Ok(new {
                success = true,
                message = "Business settings updated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BUSINESS_SETTINGS] Error updating business settings for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPatch("business-settings")]
    public async Task<ActionResult<BusinessSettingsDto>> UpdateBusinessSettingsPartial([FromBody] UpdateBusinessSettingsRequest request)
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[BUSINESS_SETTINGS] Updating business settings (partial) for organization {OrganizationId}", organizationId);

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                _logger.LogWarning("[BUSINESS_SETTINGS] Organization {OrganizationId} not found during partial update", organizationId);
                return NotFound("Organization not found");
            }

            // Parse current business settings or create defaults
            BusinessSettingsDto currentSettings;
            if (!string.IsNullOrEmpty(organization.BusinessSettings))
            {
                try
                {
                    currentSettings = JsonSerializer.Deserialize<BusinessSettingsDto>(organization.BusinessSettings) ?? new();
                }
                catch
                {
                    currentSettings = new();
                }
            }
            else
            {
                currentSettings = new();
            }

            // Apply partial updates - only update non-null values
            if (request.DefaultSplit != null)
            {
                currentSettings.Commission.DefaultSplit = request.DefaultSplit;

                // Also update the Organization entity field
                var splitParts = request.DefaultSplit.Split('/');
                if (splitParts.Length == 2 && decimal.TryParse(splitParts[0], out var consignorPercentage))
                {
                    organization.DefaultSplitPercentage = consignorPercentage;
                }
            }

            if (request.AllowCustomSplitsPerConsignor.HasValue)
                currentSettings.Commission.AllowCustomSplitsPerConsignor = request.AllowCustomSplitsPerConsignor.Value;

            if (request.AllowCustomSplitsPerItem.HasValue)
                currentSettings.Commission.AllowCustomSplitsPerItem = request.AllowCustomSplitsPerItem.Value;

            if (request.SalesTaxRate.HasValue)
            {
                currentSettings.Tax.SalesTaxRate = request.SalesTaxRate.Value;
                organization.TaxRate = request.SalesTaxRate.Value; // Update Organization field
            }

            if (request.TaxIncludedInPrices.HasValue)
                currentSettings.Tax.TaxIncludedInPrices = request.TaxIncludedInPrices.Value;

            if (request.ChargeTaxOnShipping.HasValue)
                currentSettings.Tax.ChargeTaxOnShipping = request.ChargeTaxOnShipping.Value;

            if (request.TaxIdEin != null)
                currentSettings.Tax.TaxIdEin = request.TaxIdEin;

            if (request.HoldPeriodDays.HasValue)
                currentSettings.Payouts.HoldPeriodDays = request.HoldPeriodDays.Value;

            if (request.MinimumAmount.HasValue)
                currentSettings.Payouts.MinimumAmount = request.MinimumAmount.Value;

            if (request.PayoutMethod != null)
                currentSettings.Payouts.Method = request.PayoutMethod;

            if (request.PayoutSchedule != null)
                currentSettings.Payouts.Schedule = request.PayoutSchedule;

            if (request.AutoProcessing.HasValue)
                currentSettings.Payouts.AutoProcessing = request.AutoProcessing.Value;

            if (request.RefundPolicy != null)
                currentSettings.Payouts.RefundPolicy = request.RefundPolicy;

            if (request.RefundWindowDays.HasValue)
                currentSettings.Payouts.RefundWindowDays = request.RefundWindowDays.Value;

            if (request.DefaultConsignmentPeriodDays.HasValue)
                currentSettings.Items.DefaultConsignmentPeriodDays = request.DefaultConsignmentPeriodDays.Value;

            if (request.EnableAutoMarkdowns.HasValue)
                currentSettings.Items.EnableAutoMarkdowns = request.EnableAutoMarkdowns.Value;

            if (request.ItemSubmissionMode != null)
                currentSettings.Items.ItemSubmissionMode = request.ItemSubmissionMode;

            if (request.AutoApproveItems.HasValue)
                currentSettings.Items.AutoApproveItems = request.AutoApproveItems.Value;

            // Serialize updated settings
            organization.BusinessSettings = JsonSerializer.Serialize(currentSettings);
            organization.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("[BUSINESS_SETTINGS] Business settings partially updated for organization {OrganizationId}", organizationId);

            return Ok(new { success = true, data = currentSettings });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BUSINESS_SETTINGS] Error partially updating business settings for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("storefront-settings")]
    public async Task<ActionResult<StorefrontSettingsDto>> GetStorefrontSettings()
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[STOREFRONT_SETTINGS] Getting storefront settings for organization {OrganizationId}", organizationId);

        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                _logger.LogWarning("[STOREFRONT_SETTINGS] Organization {OrganizationId} not found", organizationId);
                return NotFound("Organization not found");
            }

            // Parse JSON settings or create defaults
            StorefrontSettingsDto? storefrontSettings = null;
            if (!string.IsNullOrEmpty(organization.StorefrontSettings))
            {
                try
                {
                    storefrontSettings = JsonSerializer.Deserialize<StorefrontSettingsDto>(organization.StorefrontSettings);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "[STOREFRONT_SETTINGS] Failed to parse storefront settings JSON for organization {OrganizationId}", organizationId);
                }
            }

            // Create defaults based on existing Organization fields
            storefrontSettings ??= new StorefrontSettingsDto
            {
                SelectedChannel = "cg-storefront",
                Square = new SquareSettingsDto
                {
                    Connected = false,
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
                    StoreSlug = organization.Slug ?? "",
                    DnsVerified = false,
                    StripeConnected = organization.StripeConnected,
                    BannerImageUrl = organization.ShopBannerUrl,
                    PrimaryColor = "#2563eb",
                    AccentColor = "#1d4ed8",
                    MetaTitle = organization.Name,
                    MetaDescription = organization.ShopDescription
                },
                InStore = new InStoreSettingsDto
                {
                    UseReceiptNumbers = true,
                    NextReceiptNumber = 1,
                    RequireManagerApproval = false,
                    AllowLayaway = false
                }
            };

            _logger.LogDebug("[STOREFRONT_SETTINGS] Storefront settings retrieved for organization {OrganizationId}", organizationId);
            return Ok(storefrontSettings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[STOREFRONT_SETTINGS] Error getting storefront settings for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("storefront-settings")]
    public async Task<ActionResult<object>> UpdateStorefrontSettings([FromBody] StorefrontSettingsDto storefrontSettingsDto)
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[STOREFRONT_SETTINGS] Updating storefront settings for organization {OrganizationId}", organizationId);

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                _logger.LogWarning("[STOREFRONT_SETTINGS] Organization {OrganizationId} not found during update", organizationId);
                return NotFound("Organization not found");
            }

            // Update basic fields that exist in Organization entity
            if (storefrontSettingsDto.CgStorefront != null)
            {
                organization.Slug = storefrontSettingsDto.CgStorefront.StoreSlug;
                organization.ShopBannerUrl = storefrontSettingsDto.CgStorefront.BannerImageUrl;
                organization.StripeConnected = storefrontSettingsDto.CgStorefront.StripeConnected;
            }

            // Store full settings as JSON
            organization.StorefrontSettings = JsonSerializer.Serialize(storefrontSettingsDto);
            organization.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("[STOREFRONT_SETTINGS] Storefront settings updated for organization {OrganizationId}", organizationId);

            return Ok(new {
                success = true,
                message = "Storefront settings updated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[STOREFRONT_SETTINGS] Error updating storefront settings for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPatch("storefront-settings")]
    public async Task<ActionResult<object>> UpdateStorefrontSettingsPartial([FromBody] Dictionary<string, object> updates)
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[STOREFRONT_SETTINGS_PATCH] Updating storefront settings for organization {OrganizationId} with {UpdateCount} changes",
            organizationId, updates?.Count ?? 0);

        if (updates == null || updates.Count == 0)
        {
            return BadRequest("No updates provided");
        }

        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                _logger.LogWarning("[STOREFRONT_SETTINGS_PATCH] Organization {OrganizationId} not found during update", organizationId);
                return NotFound("Organization not found");
            }

            // Get existing settings or create defaults
            StorefrontSettingsDto currentSettings = null;
            if (!string.IsNullOrEmpty(organization.StorefrontSettings))
            {
                try
                {
                    currentSettings = JsonSerializer.Deserialize<StorefrontSettingsDto>(organization.StorefrontSettings);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "[STOREFRONT_SETTINGS_PATCH] Failed to parse existing storefront settings JSON for organization {OrganizationId}", organizationId);
                }
            }

            // Initialize with defaults if null
            currentSettings ??= new StorefrontSettingsDto
            {
                SelectedChannel = "cg-storefront",
                CgStorefront = new CgStorefrontSettingsDto
                {
                    StoreSlug = "",
                    BannerImageUrl = "",
                    StripeConnected = false,
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
                        FlatRate = 0,
                        FreeShippingThreshold = 0,
                        ShipsFromZipCode = ""
                    },
                    SalesSettings = new StorefrontSalesSettingsDto
                    {
                        EnableBestOffer = false,
                        AutoAcceptPercentage = 0,
                        MinimumOfferPercentage = 0
                    }
                }
            };

            // Apply updates using flat key-value pattern
            foreach (var kvp in updates)
            {
                if (ApplyStorefrontUpdate(currentSettings, kvp.Key, kvp.Value))
                {
                    _logger.LogDebug("[STOREFRONT_SETTINGS_PATCH] Applied update {Key} = {Value}", kvp.Key, kvp.Value);
                }
                else
                {
                    _logger.LogWarning("[STOREFRONT_SETTINGS_PATCH] Failed to apply update {Key} = {Value}", kvp.Key, kvp.Value);
                }
            }

            // Update organization entity fields from CgStorefront
            if (currentSettings.CgStorefront != null)
            {
                organization.Slug = currentSettings.CgStorefront.StoreSlug;
                organization.ShopBannerUrl = currentSettings.CgStorefront.BannerImageUrl;
                organization.StripeConnected = currentSettings.CgStorefront.StripeConnected;
            }

            // Store updated settings as JSON
            organization.StorefrontSettings = JsonSerializer.Serialize(currentSettings);
            organization.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("[STOREFRONT_SETTINGS_PATCH] Storefront settings updated for organization {OrganizationId}", organizationId);

            return Ok(new {
                success = true,
                data = currentSettings
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[STOREFRONT_SETTINGS_PATCH] Error updating storefront settings for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    private bool ApplyStorefrontUpdate(StorefrontSettingsDto settings, string key, object value)
    {
        try
        {
            // Handle JsonElement values from the request body
            if (value is JsonElement jsonElement)
            {
                switch (jsonElement.ValueKind)
                {
                    case JsonValueKind.String:
                        value = jsonElement.GetString();
                        break;
                    case JsonValueKind.Number:
                        if (jsonElement.TryGetInt32(out var intValue))
                            value = intValue;
                        else if (jsonElement.TryGetDecimal(out var decimalValue))
                            value = decimalValue;
                        break;
                    case JsonValueKind.True:
                        value = true;
                        break;
                    case JsonValueKind.False:
                        value = false;
                        break;
                    case JsonValueKind.Null:
                        value = null;
                        break;
                }
            }

            // Apply the update based on the key pattern
            switch (key)
            {
                // CgStorefront basic properties
                case "storeSlug":
                    if (value is string slug) settings.CgStorefront.StoreSlug = slug;
                    break;
                case "bannerImageUrl":
                    if (value is string banner) settings.CgStorefront.BannerImageUrl = banner;
                    break;
                case "stripeConnected":
                    if (value is bool stripe) settings.CgStorefront.StripeConnected = stripe;
                    break;

                // Payment settings
                case "paymentSettings.enableCreditCards":
                    if (value is bool enableCards) settings.CgStorefront.PaymentSettings.EnableCreditCards = enableCards;
                    break;
                case "paymentSettings.enableBuyNow":
                    if (value is bool enableBuyNow) settings.CgStorefront.PaymentSettings.EnableBuyNow = enableBuyNow;
                    break;
                case "paymentSettings.enableLayaway":
                    if (value is bool enableLayaway) settings.CgStorefront.PaymentSettings.EnableLayaway = enableLayaway;
                    break;
                case "paymentSettings.layawayDepositPercentage":
                    if (value is int depositPercentage) settings.CgStorefront.PaymentSettings.LayawayDepositPercentage = depositPercentage;
                    break;
                case "paymentSettings.layawayTermsInDays":
                    if (value is int termsInDays) settings.CgStorefront.PaymentSettings.LayawayTermsInDays = termsInDays;
                    break;

                // Shipping settings
                case "shippingSettings.enableShipping":
                    if (value is bool enableShipping) settings.CgStorefront.ShippingSettings.EnableShipping = enableShipping;
                    break;
                case "shippingSettings.flatRate":
                    if (value is decimal flatRate) settings.CgStorefront.ShippingSettings.FlatRate = flatRate;
                    else if (value is int flatRateInt) settings.CgStorefront.ShippingSettings.FlatRate = flatRateInt;
                    break;
                case "shippingSettings.freeShippingThreshold":
                    if (value is decimal threshold) settings.CgStorefront.ShippingSettings.FreeShippingThreshold = threshold;
                    else if (value is int thresholdInt) settings.CgStorefront.ShippingSettings.FreeShippingThreshold = thresholdInt;
                    break;
                case "shippingSettings.shipsFromZipCode":
                    if (value is string zipCode) settings.CgStorefront.ShippingSettings.ShipsFromZipCode = zipCode;
                    break;

                // Sales settings
                case "salesSettings.enableBestOffer":
                    if (value is bool enableBestOffer) settings.CgStorefront.SalesSettings.EnableBestOffer = enableBestOffer;
                    break;
                case "salesSettings.autoAcceptPercentage":
                    if (value is int autoAcceptPercentage) settings.CgStorefront.SalesSettings.AutoAcceptPercentage = autoAcceptPercentage;
                    break;
                case "salesSettings.minimumOfferPercentage":
                    if (value is int minimumOfferPercentage) settings.CgStorefront.SalesSettings.MinimumOfferPercentage = minimumOfferPercentage;
                    break;

                default:
                    _logger.LogWarning("[STOREFRONT_SETTINGS_PATCH] Unknown storefront setting key: {Key}", key);
                    return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[STOREFRONT_SETTINGS_PATCH] Error applying update {Key} = {Value}", key, value);
            return false;
        }
    }

    [HttpPost("store-code/regenerate")]
    public async Task<ActionResult<StoreCodeRegenerationDto>> RegenerateStoreCode()
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[STORE_CODE] Regenerating store code for organization {OrganizationId}", organizationId);

        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                _logger.LogWarning("[STORE_CODE] Organization {OrganizationId} not found", organizationId);
                return NotFound("Organization not found");
            }

            var oldStoreCode = organization.StoreCode;

            // Generate new 8-character alphanumeric store code
            var newStoreCode = GenerateStoreCode();

            // Ensure uniqueness
            while (await _context.Organizations.AnyAsync(o => o.StoreCode == newStoreCode))
            {
                newStoreCode = GenerateStoreCode();
            }

            organization.StoreCode = newStoreCode;
            organization.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("[STORE_CODE] Store code regenerated for organization {OrganizationId}: {OldCode} -> {NewCode}",
                organizationId, oldStoreCode, newStoreCode);

            var result = new StoreCodeRegenerationDto
            {
                NewStoreCode = newStoreCode,
                GeneratedAt = DateTime.UtcNow
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[STORE_CODE] Error regenerating store code for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }


    private string GenerateStoreCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 8)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    [HttpGet("default-consignor-permissions")]
    public async Task<ActionResult<object>> GetDefaultConsignorPermissions()
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[CONSIGNOR_PERMISSIONS] Getting default consignor permissions for organization {OrganizationId}", organizationId);

        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                _logger.LogWarning("[CONSIGNOR_PERMISSIONS] Organization {OrganizationId} not found", organizationId);
                return NotFound("Organization not found");
            }

            // Parse JSON settings or create defaults
            object? permissions = null;
            if (!string.IsNullOrEmpty(organization.ConsignorPermissions))
            {
                try
                {
                    permissions = JsonSerializer.Deserialize<object>(organization.ConsignorPermissions);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "[CONSIGNOR_PERMISSIONS] Failed to parse consignor permissions JSON for organization {OrganizationId}", organizationId);
                }
            }

            // Create defaults if parsing failed or no settings exist
            permissions ??= new
            {
                canViewOwnItems = true,
                canEditItemDetails = true,
                canRequestPayout = true,
                canViewSalesHistory = true,
                canUpdateContactInfo = true,
                maxItemsPerSubmission = 50,
                requireApprovalForChanges = false
            };

            _logger.LogDebug("[CONSIGNOR_PERMISSIONS] Default consignor permissions retrieved for organization {OrganizationId}", organizationId);
            return Ok(permissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CONSIGNOR_PERMISSIONS] Error getting default consignor permissions for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("default-consignor-permissions")]
    public async Task<ActionResult<object>> UpdateDefaultConsignorPermissions([FromBody] object permissions)
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[CONSIGNOR_PERMISSIONS] Updating default consignor permissions for organization {OrganizationId}", organizationId);

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                _logger.LogWarning("[CONSIGNOR_PERMISSIONS] Organization {OrganizationId} not found during update", organizationId);
                return NotFound("Organization not found");
            }

            // Store permissions as JSON
            organization.ConsignorPermissions = JsonSerializer.Serialize(permissions);
            organization.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("[CONSIGNOR_PERMISSIONS] Default consignor permissions updated for organization {OrganizationId}", organizationId);

            return Ok(new {
                success = true,
                message = "Default consignor permissions updated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CONSIGNOR_PERMISSIONS] Error updating default consignor permissions for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPatch("default-consignor-permissions")]
    public async Task<ActionResult<object>> UpdateDefaultConsignorPermissionsPartial([FromBody] Dictionary<string, object> updates)
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[CONSIGNOR_PERMISSIONS] Updating default consignor permissions (partial) for organization {OrganizationId}", organizationId);

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                _logger.LogWarning("[CONSIGNOR_PERMISSIONS] Organization {OrganizationId} not found during partial update", organizationId);
                return NotFound("Organization not found");
            }

            // Parse existing permissions or create defaults
            Dictionary<string, object> currentPermissions;
            if (!string.IsNullOrEmpty(organization.ConsignorPermissions))
            {
                try
                {
                    currentPermissions = JsonSerializer.Deserialize<Dictionary<string, object>>(organization.ConsignorPermissions) ?? new();
                }
                catch
                {
                    currentPermissions = new();
                }
            }
            else
            {
                currentPermissions = new();
            }

            // Apply partial updates
            foreach (var update in updates)
            {
                currentPermissions[update.Key] = update.Value;
            }

            // Store updated permissions as JSON
            organization.ConsignorPermissions = JsonSerializer.Serialize(currentPermissions);
            organization.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("[CONSIGNOR_PERMISSIONS] Default consignor permissions partially updated for organization {OrganizationId}", organizationId);

            return Ok(currentPermissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CONSIGNOR_PERMISSIONS] Error partially updating default consignor permissions for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }


    [HttpPost("send-sample-agreement")]
    public async Task<ActionResult> SendSampleAgreement([FromBody] SendSampleAgreementRequest request)
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[SAMPLE_AGREEMENT] Sending sample agreement notification for organization {OrganizationId}", organizationId);

        try
        {
            if (string.IsNullOrEmpty(request.TemplateContent))
            {
                return BadRequest("Template content cannot be empty");
            }

            // Get organization details for placeholder replacement
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                return NotFound("Organization not found");
            }

            // Replace placeholders with actual organization data and sample consignor data
            var processedContent = ProcessAgreementTemplate(request.TemplateContent, organization);

            // Generate PDF from the processed content
            var pdfBytes = await GeneratePdfFromText(processedContent);

            // Create notification with PDF attachment
            var notification = new
            {
                Type = "sample_agreement",
                Title = "Sample Consignment Agreement Preview",
                Message = "Here's how your agreement will look when sent to consignors. Review the content and formatting to ensure it meets your needs.",
                AttachmentData = Convert.ToBase64String(pdfBytes),
                AttachmentName = "sample-agreement.pdf",
                AttachmentType = "application/pdf",
                Priority = "normal",
                CreatedAt = DateTime.UtcNow
            };

            _logger.LogInformation("[SAMPLE_AGREEMENT] Sample agreement notification created for organization {OrganizationId}", organizationId);

            return Ok(new { success = true, message = "Sample agreement sent to notifications" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SAMPLE_AGREEMENT] Error sending sample agreement for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("generate-pdf")]
    public async Task<ActionResult> GeneratePdf([FromBody] GeneratePdfRequest request)
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[PDF] Generating PDF for organization {OrganizationId}, contentType: {ContentType}", organizationId, request.ContentType);

        try
        {
            if (string.IsNullOrEmpty(request.Content))
            {
                return BadRequest("Content cannot be empty");
            }

            byte[] pdfBytes;

            if (request.ContentType?.ToLower() == "html")
            {
                // Generate PDF from HTML
                pdfBytes = await GeneratePdfFromHtml(request.Content);
            }
            else
            {
                // Generate PDF from plain text
                pdfBytes = await GeneratePdfFromText(request.Content);
            }

            _logger.LogInformation("[PDF] PDF generated successfully for organization {OrganizationId}, size: {Size} bytes", organizationId, pdfBytes.Length);

            return File(pdfBytes, "application/pdf", "agreement-template.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PDF] Error generating PDF for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("agreement-templates")]
    public async Task<ActionResult<AgreementTemplateDto>> UploadAgreementTemplate(IFormFile file)
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[AGREEMENT] Uploading agreement template for organization {OrganizationId}", organizationId);

        try
        {
            // Validate file
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded");
            }

            // Validate file type (PDF, TXT, RTF, HTML for agreements)
            var allowedTypes = new[] {
                "application/pdf",
                "text/plain",
                "text/rtf",
                "application/rtf",
                "text/html",
                "text/htm"
            };

            var isValidType = allowedTypes.Contains(file.ContentType.ToLower()) ||
                              IsValidAgreementFileExtension(file.FileName);

            if (!isValidType)
            {
                return BadRequest("Only PDF, TXT, RTF, or HTML files are allowed for agreement templates");
            }

            // Validate file size (max 5MB)
            const long maxFileSize = 5 * 1024 * 1024; // 5MB
            if (file.Length > maxFileSize)
            {
                return BadRequest("File size cannot exceed 5MB");
            }

            // Generate unique template ID
            var templateId = Guid.NewGuid().ToString();
            var safeFileName = Path.GetFileNameWithoutExtension(file.FileName);
            var fileExtension = Path.GetExtension(file.FileName);
            var storedFileName = $"{organizationId}_{templateId}_{safeFileName}{fileExtension}";

            // Create uploads directory if it doesn't exist
            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "agreement-templates");
            Directory.CreateDirectory(uploadsPath);

            // Save file to local storage
            var filePath = Path.Combine(uploadsPath, storedFileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            _logger.LogInformation("[AGREEMENT] Agreement template saved to {FilePath} for organization {OrganizationId}, templateId: {TemplateId}", filePath, organizationId, templateId);

            var result = new AgreementTemplateDto
            {
                Id = templateId,
                FileName = file.FileName,
                FileSize = file.Length,
                UploadedAt = DateTime.UtcNow,
                ContentType = file.ContentType
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AGREEMENT] Error uploading agreement template for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("agreement-templates/{templateId}")]
    public async Task<ActionResult> DownloadAgreementTemplate(string templateId)
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[AGREEMENT] Downloading agreement template {TemplateId} for organization {OrganizationId}", templateId, organizationId);

        try
        {
            // Find the file in uploads directory
            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "agreement-templates");
            var files = Directory.GetFiles(uploadsPath, $"{organizationId}_{templateId}_*");

            if (!files.Any())
            {
                _logger.LogWarning("[AGREEMENT] Agreement template {TemplateId} not found for organization {OrganizationId}", templateId, organizationId);
                return NotFound("Agreement template not found");
            }

            var filePath = files.First();
            var fileInfo = new FileInfo(filePath);

            // Extract original filename from stored filename
            var storedFileName = Path.GetFileNameWithoutExtension(fileInfo.Name);
            var parts = storedFileName.Split('_', 3);
            var originalFileName = parts.Length > 2 ? parts[2] + fileInfo.Extension : fileInfo.Name;

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);

            _logger.LogInformation("[AGREEMENT] Agreement template {TemplateId} downloaded for organization {OrganizationId}", templateId, organizationId);

            return File(fileBytes, "application/pdf", originalFileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AGREEMENT] Error downloading agreement template {TemplateId} for organization {OrganizationId}", templateId, organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("agreement-templates/{templateId}/text")]
    public async Task<ActionResult<string>> GetAgreementTemplateAsText(string templateId)
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[AGREEMENT] Getting agreement template text {TemplateId} for organization {OrganizationId}", templateId, organizationId);

        try
        {
            // Find the file in uploads directory
            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "agreement-templates");
            var files = Directory.GetFiles(uploadsPath, $"{organizationId}_{templateId}_*");

            if (!files.Any())
            {
                _logger.LogWarning("[AGREEMENT] Agreement template {TemplateId} not found for organization {OrganizationId}", templateId, organizationId);
                return NotFound("Agreement template not found");
            }

            var filePath = files.First();
            var fileInfo = new FileInfo(filePath);

            // Check if it's a text file that we can read
            var textExtensions = new[] { ".txt", ".html", ".htm", ".rtf" };
            var extension = fileInfo.Extension.ToLower();

            if (!textExtensions.Contains(extension))
            {
                _logger.LogWarning("[AGREEMENT] Agreement template {TemplateId} is not a text file (extension: {Extension})", templateId, extension);
                return BadRequest("Agreement template is not a text file that can be displayed inline");
            }

            // Read the text content
            var textContent = await System.IO.File.ReadAllTextAsync(filePath);

            _logger.LogInformation("[AGREEMENT] Agreement template text {TemplateId} retrieved for organization {OrganizationId}", templateId, organizationId);

            return Content(textContent, "text/plain");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AGREEMENT] Error getting agreement template text {TemplateId} for organization {OrganizationId}", templateId, organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("agreement-templates/{templateId}")]
    public async Task<ActionResult> DeleteAgreementTemplate(string templateId)
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[AGREEMENT] Deleting agreement template {TemplateId} for organization {OrganizationId}", templateId, organizationId);

        try
        {
            // Find the file in uploads directory
            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "agreement-templates");
            var files = Directory.GetFiles(uploadsPath, $"{organizationId}_{templateId}_*");

            if (!files.Any())
            {
                _logger.LogWarning("[AGREEMENT] Agreement template {TemplateId} not found for organization {OrganizationId}", templateId, organizationId);
                return NotFound("Agreement template not found");
            }

            // Delete the file
            var filePath = files.First();
            System.IO.File.Delete(filePath);

            _logger.LogInformation("[AGREEMENT] Agreement template {TemplateId} deleted for organization {OrganizationId}", templateId, organizationId);

            return Ok(new { success = true, message = "Agreement template deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AGREEMENT] Error deleting agreement template {TemplateId} for organization {OrganizationId}", templateId, organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    private async Task<byte[]> GeneratePdfFromText(string textContent)
    {
        // Simple text-to-PDF generation
        // For production, consider using a library like iText7, PdfSharp, or DinkToPdf

        var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{
            font-family: Arial, sans-serif;
            line-height: 1.6;
            margin: 40px;
            color: #333;
        }}
        .header {{
            text-align: center;
            margin-bottom: 30px;
            border-bottom: 2px solid #333;
            padding-bottom: 20px;
        }}
        .content {{
            white-space: pre-wrap;
            font-size: 12px;
        }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>Consignment Agreement</h1>
    </div>
    <div class='content'>{textContent.Replace("<", "&lt;").Replace(">", "&gt;")}</div>
</body>
</html>";

        return await GeneratePdfFromHtml(html);
    }

    private async Task<byte[]> GeneratePdfFromHtml(string htmlContent)
    {
        // TODO: Implement proper HTML to PDF conversion
        // For now, return a simple PDF with basic text
        // In production, use libraries like:
        // - DinkToPdf (wkhtmltopdf wrapper)
        // - PuppeteerSharp (headless Chrome)
        // - iText7 with HTML conversion
        // - SelectPdf

        // For demonstration, create a simple PDF with basic content
        return await CreateSimplePdf(htmlContent);
    }

    private async Task<byte[]> CreateSimplePdf(string content)
    {
        // This is a placeholder implementation
        // In a real application, you would use a proper PDF library

        // Extract text content from HTML for simple implementation
        var textContent = System.Text.RegularExpressions.Regex.Replace(content, "<.*?>", string.Empty);
        textContent = System.Net.WebUtility.HtmlDecode(textContent);

        // Create a simple PDF using iText7 or similar
        using var stream = new MemoryStream();

        // TODO: Replace with actual PDF generation library
        // This is just a placeholder that creates a text file with PDF header
        var pdfContent = $@"%PDF-1.4
1 0 obj
<</Type/Catalog/Pages 2 0 R>>
endobj
2 0 obj
<</Type/Pages/Kids[3 0 R]/Count 1>>
endobj
3 0 obj
<</Type/Page/Parent 2 0 R/MediaBox[0 0 612 792]/Contents 4 0 R>>
endobj
4 0 obj
<</Length {textContent.Length + 50}>>
stream
BT
/F1 12 Tf
50 750 Td
({textContent}) Tj
ET
endstream
endobj
xref
0 5
0000000000 65535 f
0000000009 00000 n
0000000058 00000 n
0000000115 00000 n
0000000202 00000 n
trailer
<</Size 5/Root 1 0 R>>
startxref
{300 + textContent.Length}
%%EOF";

        var bytes = System.Text.Encoding.UTF8.GetBytes(pdfContent);
        await stream.WriteAsync(bytes);

        return stream.ToArray();
    }

    [HttpGet("agreement-template-id")]
    public async Task<ActionResult<object>> GetAgreementTemplateId()
    {
        var organizationId = GetOrganizationId();

        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                return NotFound("Organization not found");
            }

            return Ok(new { agreementTemplateId = string.IsNullOrEmpty(organization.AgreementTemplateCloudinaryUrl) ? null : organization.Id.ToString() });
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Internal server error");
        }
    }

    private Guid GetOrganizationId()
    {
        var organizationIdClaim = User.FindFirst("organizationId")?.Value;
        if (organizationIdClaim != null && Guid.TryParse(organizationIdClaim, out var organizationId))
        {
            return organizationId;
        }

        throw new UnauthorizedAccessException("Organization ID not found in token");
    }

    private string ProcessAgreementTemplate(string template, ConsignmentGenie.Core.Entities.Organization organization)
    {
        // Replace placeholders with actual organization data and sample consignor data
        var processedTemplate = template
            .Replace("{{SHOP_NAME}}", organization.Name ?? "Your Shop Name")
            .Replace("{{SHOP_ADDRESS}}", FormatShopAddress(organization))
            .Replace("{{CONSIGNOR_NAME}}", "Jane Smith") // Sample consignor
            .Replace("{{CONSIGNOR_EMAIL}}", "jane.smith@example.com") // Sample email
            .Replace("{{COMMISSION_PERCENT}}", GetCommissionPercent(organization))
            .Replace("{{CONSIGNOR_PERCENT}}", GetConsignorPercent(organization))
            .Replace("{{CONSIGNMENT_PERIOD}}", "90") // Default or from settings
            .Replace("{{RETRIEVAL_PERIOD}}", "30") // Default or from settings
            .Replace("{{UNSOLD_POLICY}}", "Items become property of the shop") // Default or from settings
            .Replace("{{CURRENT_DATE}}", DateTime.Now.ToString("MMMM dd, yyyy"))
            .Replace("{{AGREEMENT_DATE}}", DateTime.Now.ToString("MMMM dd, yyyy"));

        return processedTemplate;
    }

    private string FormatShopAddress(ConsignmentGenie.Core.Entities.Organization organization)
    {
        var addressParts = new List<string>();

        if (!string.IsNullOrEmpty(organization.ShopAddress1))
            addressParts.Add(organization.ShopAddress1);

        if (!string.IsNullOrEmpty(organization.ShopAddress2))
            addressParts.Add(organization.ShopAddress2);

        var cityStateZip = new List<string>();
        if (!string.IsNullOrEmpty(organization.ShopCity))
            cityStateZip.Add(organization.ShopCity);
        if (!string.IsNullOrEmpty(organization.ShopState))
            cityStateZip.Add(organization.ShopState);
        if (!string.IsNullOrEmpty(organization.ShopZip))
            cityStateZip.Add(organization.ShopZip);

        if (cityStateZip.Count > 0)
            addressParts.Add(string.Join(", ", cityStateZip));

        return addressParts.Count > 0 ? string.Join(", ", addressParts) : "Your Shop Address";
    }

    private string GetCommissionPercent(ConsignmentGenie.Core.Entities.Organization organization)
    {
        // Calculate shop commission from consignor percentage
        var consignorPercent = organization.DefaultSplitPercentage;
        var shopPercent = 100 - consignorPercent;
        return shopPercent.ToString("F0");
    }

    private string GetConsignorPercent(ConsignmentGenie.Core.Entities.Organization organization)
    {
        return organization.DefaultSplitPercentage.ToString("F0");
    }

    private bool IsValidAgreementFileExtension(string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return false;

        var allowedExtensions = new[] { ".pdf", ".txt", ".rtf", ".html", ".htm" };
        var fileExtension = Path.GetExtension(fileName).ToLower();
        return allowedExtensions.Contains(fileExtension);
    }

    // ===== NOTIFICATION SETTINGS ENDPOINTS =====

    [HttpGet("notification-settings")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<NotificationSettingsDto>> GetNotificationSettings()
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[NOTIFICATION_SETTINGS] Getting notification settings for organization {OrganizationId}", organizationId);

        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                _logger.LogWarning("[NOTIFICATION_SETTINGS] Organization {OrganizationId} not found", organizationId);
                return NotFound("Organization not found");
            }

            NotificationSettingsDto? notificationSettings = null;
            if (!string.IsNullOrEmpty(organization.NotificationSettings))
            {
                try
                {
                    notificationSettings = JsonSerializer.Deserialize<NotificationSettingsDto>(organization.NotificationSettings);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "[NOTIFICATION_SETTINGS] Failed to parse notification settings JSON for organization {OrganizationId}", organizationId);
                }
            }

            // Return default settings if none exist
            notificationSettings ??= new NotificationSettingsDto
            {
                PrimaryEmail = organization.ShopEmail ?? "",
                PhoneNumber = organization.ShopPhone,
                EmailPreferences = new Dictionary<string, bool>
                {
                    ["item_sold"] = true,
                    ["daily_sales_summary"] = false,
                    ["weekly_report"] = true,
                    ["monthly_statement"] = true,
                    ["consignor_signup"] = true,
                    ["consignor_item_added"] = false,
                    ["pending_approval"] = true,
                    ["consignor_payout_ready"] = true,
                    ["high_value_sale"] = true,
                    ["low_inventory"] = true,
                    ["pricing_suggestions"] = false,
                    ["system_maintenance"] = true,
                    ["security_alerts"] = true,
                    ["account_changes"] = true,
                    ["backup_status"] = false
                },
                SmsPreferences = new Dictionary<string, bool>
                {
                    ["item_sold"] = false,
                    ["daily_sales_summary"] = false,
                    ["weekly_report"] = false,
                    ["monthly_statement"] = false,
                    ["consignor_signup"] = false,
                    ["consignor_item_added"] = false,
                    ["pending_approval"] = false,
                    ["consignor_payout_ready"] = false,
                    ["high_value_sale"] = false,
                    ["low_inventory"] = false,
                    ["pricing_suggestions"] = false,
                    ["system_maintenance"] = false,
                    ["security_alerts"] = true,
                    ["account_changes"] = true,
                    ["backup_status"] = false
                },
                Thresholds = new NotificationThresholdsDto
                {
                    HighValueSale = 500m,
                    LowInventory = 10
                }
            };

            _logger.LogDebug("[NOTIFICATION_SETTINGS] Notification settings retrieved for organization {OrganizationId}", organizationId);
            return Ok(notificationSettings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NOTIFICATION_SETTINGS] Error getting notification settings for organization {OrganizationId}", organizationId);
            return StatusCode(500, "An error occurred while retrieving notification settings");
        }
    }

    [HttpPatch("notification-settings")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<NotificationSettingsDto>> UpdateNotificationSettingsPartial([FromBody] Dictionary<string, object> updates)
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[NOTIFICATION_SETTINGS] Updating notification settings (partial) for organization {OrganizationId}", organizationId);

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                _logger.LogWarning("[NOTIFICATION_SETTINGS] Organization {OrganizationId} not found during partial update", organizationId);
                return NotFound("Organization not found");
            }

            // Get existing settings or create defaults
            NotificationSettingsDto currentSettings;
            if (!string.IsNullOrEmpty(organization.NotificationSettings))
            {
                try
                {
                    currentSettings = JsonSerializer.Deserialize<NotificationSettingsDto>(organization.NotificationSettings) ?? new NotificationSettingsDto();
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "[NOTIFICATION_SETTINGS] Failed to parse existing settings, using defaults");
                    currentSettings = new NotificationSettingsDto();
                }
            }
            else
            {
                currentSettings = new NotificationSettingsDto
                {
                    PrimaryEmail = organization.ShopEmail ?? "",
                    PhoneNumber = organization.ShopPhone,
                    EmailPreferences = new Dictionary<string, bool>(),
                    SmsPreferences = new Dictionary<string, bool>(),
                    Thresholds = new NotificationThresholdsDto()
                };
            }

            // Apply partial updates
            foreach (var update in updates)
            {
                var key = update.Key;
                var value = update.Value;

                try
                {
                    if (key == "PrimaryEmail" && value is string emailValue)
                    {
                        currentSettings.PrimaryEmail = emailValue;
                    }
                    else if (key == "PhoneNumber" && value is string phoneValue)
                    {
                        currentSettings.PhoneNumber = phoneValue;
                    }
                    else if (key == "HighValueSaleThreshold" && value is JsonElement thresholdElement && thresholdElement.TryGetDecimal(out var thresholdValue))
                    {
                        currentSettings.Thresholds.HighValueSale = thresholdValue;
                    }
                    else if (key == "LowInventoryThreshold" && value is JsonElement inventoryElement && inventoryElement.TryGetInt32(out var inventoryValue))
                    {
                        currentSettings.Thresholds.LowInventory = inventoryValue;
                    }
                    else if (key.StartsWith("Email_"))
                    {
                        var notificationKey = key.Replace("Email_", "").ToLowerInvariant();
                        if (value is JsonElement boolElement && boolElement.ValueKind == JsonValueKind.True)
                        {
                            currentSettings.EmailPreferences[notificationKey] = true;
                        }
                        else if (value is JsonElement boolElement2 && boolElement2.ValueKind == JsonValueKind.False)
                        {
                            currentSettings.EmailPreferences[notificationKey] = false;
                        }
                        else if (value is bool boolValue)
                        {
                            currentSettings.EmailPreferences[notificationKey] = boolValue;
                        }
                    }
                    else if (key.StartsWith("Sms_"))
                    {
                        var notificationKey = key.Replace("Sms_", "").ToLowerInvariant();
                        if (value is JsonElement boolElement && boolElement.ValueKind == JsonValueKind.True)
                        {
                            currentSettings.SmsPreferences[notificationKey] = true;
                        }
                        else if (value is JsonElement boolElement2 && boolElement2.ValueKind == JsonValueKind.False)
                        {
                            currentSettings.SmsPreferences[notificationKey] = false;
                        }
                        else if (value is bool boolValue)
                        {
                            currentSettings.SmsPreferences[notificationKey] = boolValue;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[NOTIFICATION_SETTINGS] Failed to apply update for key {Key}", key);
                }
            }

            // Save updated settings
            organization.NotificationSettings = JsonSerializer.Serialize(currentSettings);
            await _context.SaveChangesAsync();

            _logger.LogInformation("[NOTIFICATION_SETTINGS] Notification settings updated for organization {OrganizationId}", organizationId);

            return Ok(new { success = true, data = currentSettings });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NOTIFICATION_SETTINGS] Error updating notification settings for organization {OrganizationId}", organizationId);
            return StatusCode(500, "An error occurred while updating notification settings");
        }
    }

    // ===== BUSINESS POLICIES ENDPOINTS =====

    [HttpGet("business-policies")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<BusinessPoliciesDto>> GetBusinessPolicies()
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[BUSINESS_POLICIES] Getting business policies for organization {OrganizationId}", organizationId);

        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                _logger.LogWarning("[BUSINESS_POLICIES] Organization {OrganizationId} not found", organizationId);
                return NotFound("Organization not found");
            }

            // Parse existing business policies from JSON or create defaults
            BusinessPoliciesDto businessPolicies;
            if (!string.IsNullOrEmpty(organization.BusinessSettings))
            {
                try
                {
                    var businessSettings = JsonSerializer.Deserialize<Dictionary<string, object>>(organization.BusinessSettings);
                    if (businessSettings?.ContainsKey("BusinessPolicies") == true)
                    {
                        var policiesJson = businessSettings["BusinessPolicies"].ToString();
                        businessPolicies = JsonSerializer.Deserialize<BusinessPoliciesDto>(policiesJson) ?? CreateDefaultBusinessPolicies();
                    }
                    else
                    {
                        businessPolicies = CreateDefaultBusinessPolicies();
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "[BUSINESS_POLICIES] Failed to parse business settings JSON");
                    businessPolicies = CreateDefaultBusinessPolicies();
                }
            }
            else
            {
                businessPolicies = CreateDefaultBusinessPolicies();
            }

            _logger.LogDebug("[BUSINESS_POLICIES] Business policies retrieved for organization {OrganizationId}", organizationId);
            return Ok(businessPolicies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BUSINESS_POLICIES] Error getting business policies for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("business-policies")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult> UpdateBusinessPolicies([FromBody] BusinessPoliciesDto businessPolicies)
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[BUSINESS_POLICIES] Updating business policies for organization {OrganizationId}", organizationId);

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                _logger.LogWarning("[BUSINESS_POLICIES] Organization {OrganizationId} not found during update", organizationId);
                return NotFound("Organization not found");
            }

            // Update timestamp
            businessPolicies.LastUpdated = DateTime.UtcNow;

            // Get existing business settings or create new
            Dictionary<string, object> businessSettings;
            if (!string.IsNullOrEmpty(organization.BusinessSettings))
            {
                try
                {
                    businessSettings = JsonSerializer.Deserialize<Dictionary<string, object>>(organization.BusinessSettings) ?? new();
                }
                catch
                {
                    businessSettings = new();
                }
            }
            else
            {
                businessSettings = new();
            }

            // Update the business policies section
            businessSettings["BusinessPolicies"] = businessPolicies;

            // Save back to organization
            organization.BusinessSettings = JsonSerializer.Serialize(businessSettings);
            organization.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("[BUSINESS_POLICIES] Business policies updated for organization {OrganizationId}", organizationId);
            return Ok(new { success = true, message = "Business policies updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BUSINESS_POLICIES] Error updating business policies for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    // ===== RECEIPT SETTINGS ENDPOINTS =====

    [HttpGet("receipt-settings")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<ReceiptSettingsDto>> GetReceiptSettings()
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[RECEIPT_SETTINGS] Getting receipt settings for organization {OrganizationId}", organizationId);

        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                _logger.LogWarning("[RECEIPT_SETTINGS] Organization {OrganizationId} not found", organizationId);
                return NotFound("Organization not found");
            }

            // Parse existing receipt settings from JSON or create defaults
            ReceiptSettingsDto receiptSettings;
            if (!string.IsNullOrEmpty(organization.BusinessSettings))
            {
                try
                {
                    var businessSettings = JsonSerializer.Deserialize<Dictionary<string, object>>(organization.BusinessSettings);
                    if (businessSettings?.ContainsKey("ReceiptSettings") == true)
                    {
                        var settingsJson = businessSettings["ReceiptSettings"].ToString();
                        receiptSettings = JsonSerializer.Deserialize<ReceiptSettingsDto>(settingsJson) ?? CreateDefaultReceiptSettings();
                    }
                    else
                    {
                        receiptSettings = CreateDefaultReceiptSettings();
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "[RECEIPT_SETTINGS] Failed to parse business settings JSON");
                    receiptSettings = CreateDefaultReceiptSettings();
                }
            }
            else
            {
                receiptSettings = CreateDefaultReceiptSettings();
            }

            _logger.LogDebug("[RECEIPT_SETTINGS] Receipt settings retrieved for organization {OrganizationId}", organizationId);
            return Ok(receiptSettings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RECEIPT_SETTINGS] Error getting receipt settings for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("receipt-settings")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult> UpdateReceiptSettings([FromBody] ReceiptSettingsDto receiptSettings)
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[RECEIPT_SETTINGS] Updating receipt settings for organization {OrganizationId}", organizationId);

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                _logger.LogWarning("[RECEIPT_SETTINGS] Organization {OrganizationId} not found during update", organizationId);
                return NotFound("Organization not found");
            }

            // Update timestamp
            receiptSettings.LastUpdated = DateTime.UtcNow;

            // Get existing business settings or create new
            Dictionary<string, object> businessSettings;
            if (!string.IsNullOrEmpty(organization.BusinessSettings))
            {
                try
                {
                    businessSettings = JsonSerializer.Deserialize<Dictionary<string, object>>(organization.BusinessSettings) ?? new();
                }
                catch
                {
                    businessSettings = new();
                }
            }
            else
            {
                businessSettings = new();
            }

            // Update the receipt settings section
            businessSettings["ReceiptSettings"] = receiptSettings;

            // Save back to organization
            organization.BusinessSettings = JsonSerializer.Serialize(businessSettings);
            organization.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("[RECEIPT_SETTINGS] Receipt settings updated for organization {OrganizationId}", organizationId);
            return Ok(new { success = true, message = "Receipt settings updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RECEIPT_SETTINGS] Error updating receipt settings for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("branding")]
    public async Task<ActionResult<object>> GetBranding()
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[BRANDING] Getting branding settings for organization {OrganizationId}", organizationId);

        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                _logger.LogWarning("[BRANDING] Organization {OrganizationId} not found", organizationId);
                return NotFound("Organization not found");
            }

            BrandingDto brandingSettings = null;
            if (!string.IsNullOrEmpty(organization.BrandingSettings))
            {
                try
                {
                    brandingSettings = JsonSerializer.Deserialize<BrandingDto>(organization.BrandingSettings);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "[BRANDING] Failed to parse existing branding settings JSON for organization {OrganizationId}", organizationId);
                }
            }

            // Initialize with defaults if null
            brandingSettings ??= CreateDefaultBrandingSettings();

            _logger.LogInformation("[BRANDING] Successfully retrieved branding settings for organization {OrganizationId}", organizationId);
            return Ok(brandingSettings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BRANDING] Error getting branding settings for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("branding")]
    public async Task<ActionResult<object>> UpdateBranding([FromBody] UpdateBrandingRequest request)
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[BRANDING] Updating branding settings for organization {OrganizationId}", organizationId);

        if (request == null)
        {
            return BadRequest("Request body cannot be null");
        }

        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                _logger.LogWarning("[BRANDING] Organization {OrganizationId} not found during update", organizationId);
                return NotFound("Organization not found");
            }

            // Get existing settings or create defaults
            BrandingDto currentSettings = null;
            if (!string.IsNullOrEmpty(organization.BrandingSettings))
            {
                try
                {
                    currentSettings = JsonSerializer.Deserialize<BrandingDto>(organization.BrandingSettings);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "[BRANDING] Failed to parse existing branding settings JSON for organization {OrganizationId}", organizationId);
                }
            }

            // Initialize with defaults if null
            currentSettings ??= CreateDefaultBrandingSettings();

            // Apply partial updates
            if (request.Logo != null)
            {
                currentSettings.Logo = request.Logo;
            }

            if (request.Colors != null)
            {
                currentSettings.Colors = request.Colors;
            }

            if (request.Typography != null)
            {
                currentSettings.Typography = request.Typography;
            }

            if (request.Style != null)
            {
                currentSettings.Style = request.Style;
            }

            // Update timestamp
            currentSettings.LastUpdated = DateTime.UtcNow;

            // Update organization entity fields
            if (currentSettings.Logo != null)
            {
                organization.ShopLogoUrl = currentSettings.Logo.Url;
            }

            // Store updated settings as JSON
            organization.BrandingSettings = JsonSerializer.Serialize(currentSettings);

            await _context.SaveChangesAsync();

            _logger.LogInformation("[BRANDING] Successfully updated branding settings for organization {OrganizationId}", organizationId);
            return Ok(new { success = true, data = currentSettings });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BRANDING] Error updating branding settings for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("branding/logo")]
    public async Task<ActionResult<object>> UploadLogo(IFormFile logo)
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[BRANDING_LOGO] Uploading logo for organization {OrganizationId}", organizationId);

        if (logo == null || logo.Length == 0)
        {
            return BadRequest("No logo file provided");
        }

        try
        {
            // Upload to Cloudinary
            using var fileStream = logo.OpenReadStream();
            var (logoUrl, width, height) = await _cloudinaryPhotoService.UploadLogoAsync(organizationId, fileStream, logo.FileName);

            // Update branding settings in database
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                _logger.LogWarning("[BRANDING_LOGO] Organization {OrganizationId} not found", organizationId);
                return NotFound("Organization not found");
            }

            // Parse existing branding settings or create new
            BrandingDto currentSettings;
            if (!string.IsNullOrEmpty(organization.BrandingSettings))
            {
                try
                {
                    currentSettings = JsonSerializer.Deserialize<BrandingDto>(organization.BrandingSettings) ?? CreateDefaultBrandingSettings();
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "[BRANDING_LOGO] Failed to parse existing branding settings JSON for organization {OrganizationId}, using defaults", organizationId);
                    currentSettings = CreateDefaultBrandingSettings();
                }
            }
            else
            {
                currentSettings = CreateDefaultBrandingSettings();
            }

            // Update logo information
            currentSettings.Logo.Url = logoUrl;
            currentSettings.Logo.FileName = logo.FileName;
            currentSettings.Logo.UploadedAt = DateTime.UtcNow;
            currentSettings.Logo.Dimensions = new LogoDimensionsDto
            {
                Width = width,
                Height = height
            };
            currentSettings.LastUpdated = DateTime.UtcNow;

            // Save to database
            organization.BrandingSettings = JsonSerializer.Serialize(currentSettings);
            organization.ShopLogoUrl = logoUrl; // Also update the legacy field for backward compatibility
            await _context.SaveChangesAsync();

            // Create response
            var response = new LogoUploadResponse
            {
                Url = logoUrl,
                Dimensions = new LogoDimensionsDto
                {
                    Width = width,
                    Height = height
                }
            };

            _logger.LogInformation("[BRANDING_LOGO] Successfully uploaded logo for organization {OrganizationId}: {LogoUrl}", organizationId, logoUrl);
            return Ok(new { success = true, data = response });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("[BRANDING_LOGO] Invalid logo upload for organization {OrganizationId}: {Error}", organizationId, ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BRANDING_LOGO] Error uploading logo for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("branding/logo")]
    public async Task<ActionResult<object>> RemoveLogo()
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[BRANDING_LOGO] Removing logo for organization {OrganizationId}", organizationId);

        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                _logger.LogWarning("[BRANDING_LOGO] Organization {OrganizationId} not found", organizationId);
                return NotFound("Organization not found");
            }

            // Get existing branding settings
            BrandingDto currentSettings = null;
            if (!string.IsNullOrEmpty(organization.BrandingSettings))
            {
                try
                {
                    currentSettings = JsonSerializer.Deserialize<BrandingDto>(organization.BrandingSettings);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "[BRANDING_LOGO] Failed to parse existing branding settings JSON for organization {OrganizationId}", organizationId);
                }
            }

            currentSettings ??= CreateDefaultBrandingSettings();

            // Delete logo from Cloudinary if it exists
            if (!string.IsNullOrEmpty(currentSettings.Logo?.Url))
            {
                try
                {
                    await _cloudinaryPhotoService.DeleteLogoAsync(currentSettings.Logo.Url);
                    _logger.LogInformation("[BRANDING_LOGO] Logo deleted from Cloudinary for organization {OrganizationId}", organizationId);
                }
                catch (Exception cloudinaryEx)
                {
                    _logger.LogWarning(cloudinaryEx, "[BRANDING_LOGO] Failed to delete logo from Cloudinary for organization {OrganizationId}, continuing with database update", organizationId);
                }
            }

            // Clear logo settings
            currentSettings.Logo.Url = null;
            currentSettings.Logo.FileName = null;
            currentSettings.Logo.UploadedAt = null;
            currentSettings.Logo.Dimensions = new LogoDimensionsDto { Width = 0, Height = 0 };
            currentSettings.LastUpdated = DateTime.UtcNow;

            // Update organization entity
            organization.ShopLogoUrl = null;
            organization.BrandingSettings = JsonSerializer.Serialize(currentSettings);

            await _context.SaveChangesAsync();

            _logger.LogInformation("[BRANDING_LOGO] Successfully removed logo for organization {OrganizationId}", organizationId);
            return Ok(new { success = true, message = "Logo removed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BRANDING_LOGO] Error removing logo for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("consignor-onboarding")]
    public async Task<ActionResult<object>> GetConsignorOnboarding()
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[CONSIGNOR_ONBOARDING] Getting consignor onboarding settings for organization {OrganizationId}", organizationId);

        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                _logger.LogWarning("[CONSIGNOR_ONBOARDING] Organization {OrganizationId} not found", organizationId);
                return NotFound("Organization not found");
            }

            ConsignorOnboardingDto onboardingSettings = null;
            if (!string.IsNullOrEmpty(organization.ConsignorSettings))
            {
                try
                {
                    onboardingSettings = JsonSerializer.Deserialize<ConsignorOnboardingDto>(organization.ConsignorSettings);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "[CONSIGNOR_ONBOARDING] Failed to parse existing consignor settings JSON for organization {OrganizationId}", organizationId);
                }
            }

            // Initialize with defaults if null
            onboardingSettings ??= CreateDefaultConsignorOnboardingSettings();

            _logger.LogInformation("[CONSIGNOR_ONBOARDING] Successfully retrieved consignor onboarding settings for organization {OrganizationId}", organizationId);
            return Ok(onboardingSettings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CONSIGNOR_ONBOARDING] Error getting consignor onboarding settings for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("consignor-onboarding")]
    public async Task<ActionResult<object>> UpdateConsignorOnboarding([FromBody] UpdateConsignorOnboardingRequest request)
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[CONSIGNOR_ONBOARDING] Updating consignor onboarding settings for organization {OrganizationId}", organizationId);

        if (request == null)
        {
            return BadRequest("Request body cannot be null");
        }

        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                _logger.LogWarning("[CONSIGNOR_ONBOARDING] Organization {OrganizationId} not found during update", organizationId);
                return NotFound("Organization not found");
            }

            // Get existing settings or create defaults
            ConsignorOnboardingDto currentSettings = null;
            if (!string.IsNullOrEmpty(organization.ConsignorSettings))
            {
                try
                {
                    currentSettings = JsonSerializer.Deserialize<ConsignorOnboardingDto>(organization.ConsignorSettings);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "[CONSIGNOR_ONBOARDING] Failed to parse existing consignor settings JSON for organization {OrganizationId}", organizationId);
                }
            }

            // Initialize with defaults if null
            currentSettings ??= CreateDefaultConsignorOnboardingSettings();

            // Apply partial updates
            if (request.AgreementRequirement.HasValue)
            {
                currentSettings.AgreementRequirement = request.AgreementRequirement.Value;
            }

            if (request.AgreementTemplateId.HasValue)
            {
                currentSettings.AgreementTemplateId = request.AgreementTemplateId.Value;
            }

            if (!string.IsNullOrEmpty(request.AcknowledgeTermsText))
            {
                currentSettings.AcknowledgeTermsText = request.AcknowledgeTermsText;
            }

            if (request.ApprovalMode.HasValue)
            {
                currentSettings.ApprovalMode = request.ApprovalMode.Value;
            }

            // Update timestamp
            currentSettings.LastUpdated = DateTime.UtcNow;

            // Update organization entity fields
            // RequireAgreementOnFile removed - AgreementMethod is now the single source of truth
            organization.AgreementMethod = currentSettings.AgreementRequirement.ToString().ToLower();
            organization.AcknowledgeTermsText = currentSettings.AcknowledgeTermsText;
            // Note: AgreementTemplateId is now handled through the OrganizationAgreementController upload endpoint
            organization.ApprovalMode = currentSettings.ApprovalMode;

            // Store updated settings as JSON
            organization.ConsignorSettings = JsonSerializer.Serialize(currentSettings);

            await _context.SaveChangesAsync();

            _logger.LogInformation("[CONSIGNOR_ONBOARDING] Successfully updated consignor onboarding settings for organization {OrganizationId}", organizationId);
            return Ok(new { success = true, data = currentSettings });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CONSIGNOR_ONBOARDING] Error updating consignor onboarding settings for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    // Public endpoint for customer portal - get basic organization info
    [HttpGet("public/{organizationId}")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> GetPublicOrganizationInfo(Guid organizationId)
    {
        _logger.LogInformation("[ORGANIZATION_PUBLIC] Getting public organization info for {OrganizationId}", organizationId);

        try
        {
            var organization = await _context.Organizations
                .Where(o => o.Id == organizationId)
                .Select(o => new
                {
                    o.Id,
                    o.Name,
                    o.StoreCode,
                    o.ShopLogoUrl,
                    o.ShopDescription
                })
                .FirstOrDefaultAsync();

            if (organization == null)
            {
                _logger.LogWarning("[ORGANIZATION_PUBLIC] Organization {OrganizationId} not found", organizationId);
                return NotFound("Organization not found");
            }

            _logger.LogDebug("[ORGANIZATION_PUBLIC] Public organization info retrieved for {OrganizationId}: {Name}", organizationId, organization.Name);
            return Ok(organization);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ORGANIZATION_PUBLIC] Error getting public organization info for {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    // Helper methods
    private BusinessPoliciesDto CreateDefaultBusinessPolicies()
    {
        return new BusinessPoliciesDto
        {
            StoreHours = new StoreHoursDto
            {
                Schedule = new Dictionary<string, DayScheduleDto>
                {
                    ["Monday"] = new() { IsOpen = true, OpenTime = "09:00", CloseTime = "18:00" },
                    ["Tuesday"] = new() { IsOpen = true, OpenTime = "09:00", CloseTime = "18:00" },
                    ["Wednesday"] = new() { IsOpen = true, OpenTime = "09:00", CloseTime = "18:00" },
                    ["Thursday"] = new() { IsOpen = true, OpenTime = "09:00", CloseTime = "18:00" },
                    ["Friday"] = new() { IsOpen = true, OpenTime = "09:00", CloseTime = "18:00" },
                    ["Saturday"] = new() { IsOpen = true, OpenTime = "09:00", CloseTime = "18:00" },
                    ["Sunday"] = new() { IsOpen = false, OpenTime = null, CloseTime = null }
                },
                Timezone = "EST"
            },
            Returns = new ReturnPoliciesDto
            {
                ReturnPolicy = "days",
                PeriodDays = 30,
                AllowRefunds = true,
                AllowExchanges = true,
                AllowStoreCredit = true,
                ReceiptPolicy = "required",
                NoReceiptStoreCreditOnly = false
            },
            Payments = new PaymentPoliciesDto
            {
                AcceptedMethods = new List<string> { "cash", "credit", "debit", "check" },
                LayawayAvailable = false,
                LayawayTerms = null
            },
            LastUpdated = DateTime.UtcNow
        };
    }

    private ReceiptSettingsDto CreateDefaultReceiptSettings()
    {
        return new ReceiptSettingsDto
        {
            Header = new ReceiptHeaderDto
            {
                IncludeLogo = false,
                ShowStoreInfo = true,
                ShowAddress = true,
                DateFormat = "MM/dd/yyyy",
                TimeFormat = "12h"
            },
            Content = new ReceiptContentDto
            {
                LayoutStyle = "simple",
                ShowItemDescriptions = true,
                DescriptionLength = 50,
                ShowTaxBreakdown = true,
                ShowPaymentMethod = true
            },
            Footer = new ReceiptFooterDto
            {
                CustomMessage = null,
                IncludeReturnPolicy = false,
                ReturnPolicyText = null,
                ThankYouMessage = null,
                IncludeWebsite = false
            },
            Digital = new DigitalReceiptDto
            {
                AutoEmailReceipts = false,
                EmailFormat = "html",
                PromptForEmail = false,
                EmailSubject = "Your receipt from [Store Name]",
                IncludePromoContent = false,
                PromoContent = null
            },
            Print = new PrintReceiptDto
            {
                AutoPrint = true,
                PrinterWidth = 80,
                Copies = 1,
                PrintDensity = "medium",
                LogoSize = "medium"
            },
            LastUpdated = DateTime.UtcNow
        };
    }

    private BrandingDto CreateDefaultBrandingSettings()
    {
        return new BrandingDto
        {
            Logo = new BrandingLogoDto
            {
                Url = null,
                FileName = null,
                UploadedAt = null,
                Dimensions = new LogoDimensionsDto { Width = 0, Height = 0 }
            },
            Colors = new BrandingColorsDto
            {
                Primary = "#3B82F6", // Default blue
                Secondary = "#6B7280", // Default gray
                Accent = "#10B981", // Default green
                Text = "#1F2937", // Default dark gray
                Background = "#FFFFFF" // Default white
            },
            Typography = new BrandingTypographyDto
            {
                HeadingFont = "Inter",
                BodyFont = "Inter",
                FontSizeScale = "medium"
            },
            Style = new BrandingStyleDto
            {
                Theme = "professional",
                CustomCss = null
            },
            LastUpdated = DateTime.UtcNow
        };
    }

    private ConsignorOnboardingDto CreateDefaultConsignorOnboardingSettings()
    {
        return new ConsignorOnboardingDto
        {
            AgreementRequirement = AgreementRequirement.Upload,
            AgreementTemplateId = null,
            AcknowledgeTermsText = null,
            ApprovalMode = ApprovalMode.Manual,
            LastUpdated = DateTime.UtcNow
        };
    }
}

public class GeneratePdfRequest
{
    public string Content { get; set; } = string.Empty;
    public string ContentType { get; set; } = "text"; // "text" or "html"
}

public class SendSampleAgreementRequest
{
    public string TemplateContent { get; set; } = string.Empty;
}