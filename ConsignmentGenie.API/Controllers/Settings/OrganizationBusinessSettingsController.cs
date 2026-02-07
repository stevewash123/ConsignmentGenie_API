using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ConsignmentGenie.Core.DTOs.Settings;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Application.Services;
using ConsignmentGenie.Core.Interfaces;
using System.Text.Json;

namespace ConsignmentGenie.API.Controllers.Settings;

[ApiController]
[Route("api/owner/settings/business")]
[Authorize(Roles = "Owner")]
public class OrganizationBusinessSettingsController : ControllerBase
{
    private readonly ConsignmentGenieContext _context;
    private readonly ILogger<OrganizationBusinessSettingsController> _logger;
    private readonly IOrganizationSettingsManager _organizationSettingsManager;

    public OrganizationBusinessSettingsController(
        ConsignmentGenieContext context,
        ILogger<OrganizationBusinessSettingsController> logger,
        IOrganizationSettingsManager organizationSettingsManager)
    {
        _context = context;
        _logger = logger;
        _organizationSettingsManager = organizationSettingsManager;
    }

    [HttpGet("tax-settings")]
    public async Task<ActionResult<BusinessSettingsDto>> GetTaxSettings()
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[TAX_SETTINGS] Getting tax settings for organization {OrganizationId}", organizationId);

        try
        {
            var businessPageSettings = await _organizationSettingsManager.GetPageSettingsAsync<BusinessPageSettingsDto>(organizationId, SettingsPage.Business);

            // Convert sales tax rate from storage format to percentage for display
            var businessSettings = businessPageSettings.Business;
            if (businessSettings.Tax.SalesTaxRate < 1)
            {
                businessSettings.Tax.SalesTaxRate *= 100; // Convert 0.08 to 8.0
            }

            _logger.LogDebug("[TAX_SETTINGS] Tax settings retrieved for organization {OrganizationId}", organizationId);
            return Ok(businessSettings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TAX_SETTINGS] Error getting tax settings for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }


    [HttpGet("shop-policies")]
    public async Task<ActionResult<BusinessPoliciesDto>> GetShopPolicies()
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[SHOP_POLICIES] Getting shop policies for organization {OrganizationId}", organizationId);

        try
        {
            var businessPageSettings = await _organizationSettingsManager.GetPageSettingsAsync<BusinessPageSettingsDto>(organizationId, SettingsPage.Business);

            _logger.LogDebug("[SHOP_POLICIES] Shop policies retrieved for organization {OrganizationId}", organizationId);
            return Ok(businessPageSettings.ShopPolicies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SHOP_POLICIES] Error getting shop policies for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPatch("shop-policies")]
    public async Task<ActionResult<BusinessPoliciesDto>> UpdateShopPoliciesPartial([FromBody] Dictionary<string, object> changes)
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[SHOP_POLICIES] Updating shop policies (partial) for organization {OrganizationId}. Changes keys: {Keys}", organizationId, string.Join(", ", changes.Keys));

        // Log each change separately for better debugging
        foreach (var change in changes)
        {
            _logger.LogInformation("[SHOP_POLICIES] Change detail: Key={Key}, ValueType={ValueType}, Value={Value}",
                change.Key, change.Value?.GetType().Name, JsonSerializer.Serialize(change.Value));
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            // Get current business page settings
            var businessPageSettings = await _organizationSettingsManager.GetPageSettingsAsync<BusinessPageSettingsDto>(organizationId, SettingsPage.Business);

            // Apply changes to shop policies
            foreach (var change in changes)
            {
                try
                {
                    ApplyShopPolicyChange(businessPageSettings.ShopPolicies, change.Key, change.Value);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[SHOP_POLICIES] Failed to apply change {Key}={Value}", change.Key, change.Value);
                }
            }

            // Update timestamp
            businessPageSettings.ShopPolicies.LastUpdated = DateTime.UtcNow;

            // Save updated business page settings
            await _organizationSettingsManager.SetPageSettingsAsync(organizationId, SettingsPage.Business, businessPageSettings);

            _logger.LogInformation("[SHOP_POLICIES] Shop policies partially updated for organization {OrganizationId}", organizationId);

            return Ok(new { success = true, data = businessPageSettings.ShopPolicies });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SHOP_POLICIES] Error partially updating shop policies for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPatch("tax-settings")]
    public async Task<ActionResult<TaxDto>> UpdateTaxSettingsPartial([FromBody] Dictionary<string, object> changes)
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[TAX_SETTINGS] Updating tax settings (partial) for organization {OrganizationId}", organizationId);

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            // Get current business page settings
            var businessPageSettings = await _organizationSettingsManager.GetPageSettingsAsync<BusinessPageSettingsDto>(organizationId, SettingsPage.Business);

            // Apply changes to tax settings
            foreach (var change in changes)
            {
                try
                {
                    ApplyTaxSettingChange(businessPageSettings.Business.Tax, change.Key, change.Value);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[TAX_SETTINGS] Failed to apply change {Key}={Value}", change.Key, change.Value);
                }
            }

            // Save updated business page settings
            await _organizationSettingsManager.SetPageSettingsAsync(organizationId, SettingsPage.Business, businessPageSettings);

            // Convert sales tax rate back to percentage for response
            var responseData = businessPageSettings.Business.Tax;
            if (responseData.SalesTaxRate < 1)
            {
                responseData.SalesTaxRate *= 100; // Convert 0.08 to 8.0 for display
            }

            _logger.LogInformation("[TAX_SETTINGS] Tax settings partially updated for organization {OrganizationId}", organizationId);

            return Ok(new { success = true, data = responseData });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TAX_SETTINGS] Error partially updating tax settings for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    private void ApplyShopPolicyChange(BusinessPoliciesDto policies, string key, object value)
    {
        switch (key.ToLowerInvariant())
        {
            case "storehours.schedule":
            case "schedule":
            case "storehours":
                // Handle store hours schedule updates - expecting array format [isOpen, startTime, closeTime]
                _logger.LogInformation("[SHOP_POLICIES] Processing schedule change. Key: {Key}, Value: {Value}, ValueType: {ValueType}", key, value, value?.GetType());
                if (value != null)
                {
                    // Extract schedule from storeHours object if needed
                    object scheduleData = value;
                    if (key.ToLowerInvariant() == "storehours")
                    {
                        if (value is JsonElement storeHoursElement && storeHoursElement.ValueKind == JsonValueKind.Object)
                        {
                            if (storeHoursElement.TryGetProperty("schedule", out var scheduleElement))
                            {
                                scheduleData = scheduleElement;
                                _logger.LogInformation("[SHOP_POLICIES] Extracted schedule from storeHours object");
                            }
                            else
                            {
                                _logger.LogWarning("[SHOP_POLICIES] No schedule property found in storeHours object");
                                break;
                            }
                        }
                        else if (value is Dictionary<string, object> storeHoursDict && storeHoursDict.ContainsKey("schedule"))
                        {
                            scheduleData = storeHoursDict["schedule"];
                            _logger.LogInformation("[SHOP_POLICIES] Extracted schedule from storeHours dictionary");
                        }
                        else
                        {
                            _logger.LogWarning("[SHOP_POLICIES] Cannot extract schedule from storeHours object format");
                            break;
                        }
                    }
                    try
                    {
                        var scheduleDict = new Dictionary<string, DayScheduleDto>();
                        _logger.LogInformation("[SHOP_POLICIES] Processing schedule data. Type: {Type}, Content: {Content}",
                            scheduleData?.GetType().Name, JsonSerializer.Serialize(scheduleData));

                        if (scheduleData is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Object)
                        {
                            _logger.LogInformation("[SHOP_POLICIES] Processing JsonElement with {PropertyCount} properties", jsonElement.EnumerateObject().Count());

                            foreach (var property in jsonElement.EnumerateObject())
                            {
                                _logger.LogInformation("[SHOP_POLICIES] Processing day: {Day}, ValueKind: {ValueKind}, Value: {Value}",
                                    property.Name, property.Value.ValueKind, property.Value.GetRawText());

                                if (property.Value.ValueKind == JsonValueKind.Array)
                                {
                                    var array = property.Value.EnumerateArray().ToArray();
                                    if (array.Length >= 3)
                                    {
                                        var daySchedule = new DayScheduleDto
                                        {
                                            IsOpen = array[0].GetBoolean(),
                                            OpenTime = array[1].GetString(),
                                            CloseTime = array[2].GetString()
                                        };
                                        scheduleDict[property.Name] = daySchedule;
                                        _logger.LogInformation("[SHOP_POLICIES] Added {Day} from array: IsOpen={IsOpen}, OpenTime={OpenTime}, CloseTime={CloseTime}",
                                            property.Name, daySchedule.IsOpen, daySchedule.OpenTime, daySchedule.CloseTime);
                                    }
                                }
                                else if (property.Value.ValueKind == JsonValueKind.Object)
                                {
                                    // Handle object format: {isOpen: true, openTime: "09:00", closeTime: "17:00"}
                                    var rawText = property.Value.GetRawText();
                                    _logger.LogInformation("[SHOP_POLICIES] Deserializing day schedule. Raw text: {RawText}", rawText);

                                    var options = new JsonSerializerOptions
                                    {
                                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                                    };
                                    var daySchedule = JsonSerializer.Deserialize<DayScheduleDto>(rawText, options);
                                    _logger.LogInformation("[SHOP_POLICIES] Deserialized result: IsOpen={IsOpen}, OpenTime={OpenTime}, CloseTime={CloseTime}",
                                        daySchedule?.IsOpen, daySchedule?.OpenTime, daySchedule?.CloseTime);
                                    if (daySchedule != null)
                                    {
                                        scheduleDict[property.Name] = daySchedule;
                                        _logger.LogInformation("[SHOP_POLICIES] Added {Day} from object: IsOpen={IsOpen}, OpenTime={OpenTime}, CloseTime={CloseTime}",
                                            property.Name, daySchedule.IsOpen, daySchedule.OpenTime, daySchedule.CloseTime);
                                    }
                                }
                            }
                        }
                        else if (scheduleData is Dictionary<string, object> dictObj)
                        {
                            foreach (var kvp in dictObj)
                            {
                                if (kvp.Value is JsonElement arrayElement && arrayElement.ValueKind == JsonValueKind.Array)
                                {
                                    var array = arrayElement.EnumerateArray().ToArray();
                                    if (array.Length >= 3)
                                    {
                                        var daySchedule = new DayScheduleDto
                                        {
                                            IsOpen = array[0].GetBoolean(),
                                            OpenTime = array[1].GetString(),
                                            CloseTime = array[2].GetString()
                                        };
                                        scheduleDict[kvp.Key] = daySchedule;
                                    }
                                }
                                else if (kvp.Value is object[] objArray && objArray.Length >= 3)
                                {
                                    var daySchedule = new DayScheduleDto
                                    {
                                        IsOpen = Convert.ToBoolean(objArray[0]),
                                        OpenTime = objArray[1]?.ToString(),
                                        CloseTime = objArray[2]?.ToString()
                                    };
                                    scheduleDict[kvp.Key] = daySchedule;
                                }
                                else if (kvp.Value != null)
                                {
                                    // Handle object format
                                    var json = JsonSerializer.Serialize(kvp.Value);
                                    var daySchedule = JsonSerializer.Deserialize<DayScheduleDto>(json);
                                    if (daySchedule != null)
                                    {
                                        scheduleDict[kvp.Key] = daySchedule;
                                    }
                                }
                            }
                        }

                        if (scheduleDict.Any())
                        {
                            policies.StoreHours.Schedule = scheduleDict;
                            _logger.LogInformation("[SHOP_POLICIES] Final schedule dict has {Count} entries: {Schedule}",
                                scheduleDict.Count, JsonSerializer.Serialize(scheduleDict));
                        }
                        else
                        {
                            _logger.LogWarning("[SHOP_POLICIES] No schedule entries were parsed from the input data");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "[SHOP_POLICIES] Failed to parse store hours schedule array: {Value}", value);
                    }
                }
                break;
            case "returns.returnpolicy":
            case "returnpolicy":
                if (value?.ToString() is string returnPolicy)
                    policies.Returns.ReturnPolicy = returnPolicy;
                break;
            case "returns.perioddays":
            case "perioddays":
                if (value is int periodDays || (int.TryParse(value?.ToString(), out periodDays)))
                    policies.Returns.PeriodDays = periodDays;
                break;
            case "returns.allowrefunds":
            case "allowrefunds":
                if (value is bool allowRefunds || (bool.TryParse(value?.ToString(), out allowRefunds)))
                    policies.Returns.AllowRefunds = allowRefunds;
                break;
            case "returns.allowexchanges":
            case "allowexchanges":
                if (value is bool allowExchanges || (bool.TryParse(value?.ToString(), out allowExchanges)))
                    policies.Returns.AllowExchanges = allowExchanges;
                break;
            case "returns.allowstorecredit":
            case "allowstorecredit":
                if (value is bool allowStoreCredit || (bool.TryParse(value?.ToString(), out allowStoreCredit)))
                    policies.Returns.AllowStoreCredit = allowStoreCredit;
                break;
            case "returns.receiptpolicy":
            case "receiptpolicy":
                if (value?.ToString() is string receiptPolicy)
                    policies.Returns.ReceiptPolicy = receiptPolicy;
                break;
            case "payments.acceptedmethods":
            case "acceptedmethods":
                // Handle accepted payment methods array
                break;
            case "payments.layawayavailable":
            case "layawayavailable":
                if (value is bool layawayAvailable || (bool.TryParse(value?.ToString(), out layawayAvailable)))
                    policies.Payments.LayawayAvailable = layawayAvailable;
                break;
        }
    }

    private void ApplyTaxSettingChange(TaxDto taxSettings, string key, object value)
    {
        switch (key.ToLowerInvariant())
        {
            case "salestaxrate":
                if (value is decimal salesTaxRate || (decimal.TryParse(value?.ToString(), out salesTaxRate)))
                {
                    // Convert from percentage to decimal for storage (8.0 -> 0.08)
                    if (salesTaxRate > 1)
                    {
                        salesTaxRate = salesTaxRate / 100;
                    }
                    taxSettings.SalesTaxRate = salesTaxRate;
                }
                break;
            case "taxincludedinprices":
                if (value is bool taxIncluded || (bool.TryParse(value?.ToString(), out taxIncluded)))
                    taxSettings.TaxIncludedInPrices = taxIncluded;
                break;
            case "chargetaxonshipping":
                if (value is bool chargeTaxOnShipping || (bool.TryParse(value?.ToString(), out chargeTaxOnShipping)))
                    taxSettings.ChargeTaxOnShipping = chargeTaxOnShipping;
                break;
            case "taxidein":
                if (value?.ToString() is string taxIdEin)
                    taxSettings.TaxIdEin = taxIdEin;
                break;
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
}