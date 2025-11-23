using ConsignmentGenie.API.Attributes;
using ConsignmentGenie.Application.DTOs.Reports;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportsService _reportsService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IReportsService reportsService, ILogger<ReportsController> logger)
    {
        _reportsService = reportsService;
        _logger = logger;
    }

    [HttpGet("sales")]
    [RequiresTier(SubscriptionTier.Pro)]
    public async Task<IActionResult> GetSalesReport(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] List<Guid>? providerIds = null,
        [FromQuery] List<string>? categories = null,
        [FromQuery] List<string>? paymentMethods = null)
    {
        try
        {
            var organizationId = GetOrganizationId();
            if (organizationId == Guid.Empty)
                return BadRequest("Organization not found");

            var filter = new SalesReportFilterDto
            {
                StartDate = startDate ?? DateTime.UtcNow.AddDays(-30),
                EndDate = endDate ?? DateTime.UtcNow,
                ProviderIds = providerIds,
                Categories = categories,
                PaymentMethods = paymentMethods
            };

            var result = await _reportsService.GetSalesReportAsync(organizationId, filter);

            if (!result.Success)
                return BadRequest(new { success = false, message = result.Message, errors = result.Errors });

            return Ok(new { success = true, data = result.Data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sales report");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpGet("sales/export")]
    [RequiresTier(SubscriptionTier.Pro)]
    public async Task<IActionResult> ExportSalesReport(
        [FromQuery] string format = "csv",
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] List<Guid>? providerIds = null,
        [FromQuery] List<string>? categories = null,
        [FromQuery] List<string>? paymentMethods = null)
    {
        try
        {
            var organizationId = GetOrganizationId();
            if (organizationId == Guid.Empty)
                return BadRequest("Organization not found");

            if (!new[] { "csv", "pdf" }.Contains(format.ToLower()))
                return BadRequest("Unsupported format. Use 'csv' or 'pdf'");

            var filter = new SalesReportFilterDto
            {
                StartDate = startDate ?? DateTime.UtcNow.AddDays(-30),
                EndDate = endDate ?? DateTime.UtcNow,
                ProviderIds = providerIds,
                Categories = categories,
                PaymentMethods = paymentMethods
            };

            var result = await _reportsService.ExportSalesReportAsync(organizationId, filter, format);

            if (!result.Success)
                return BadRequest(new { success = false, message = result.Message, errors = result.Errors });

            var contentType = format.ToLower() == "csv" ? "text/csv" : "application/pdf";
            var fileName = $"sales_report_{filter.StartDate:yyyy-MM-dd}_{filter.EndDate:yyyy-MM-dd}.{format}";

            return File(result.Data, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting sales report");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpGet("provider-performance")]
    [RequiresTier(SubscriptionTier.Pro)]
    public async Task<IActionResult> GetProviderPerformanceReport(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] bool includeInactive = false,
        [FromQuery] int? minItemsThreshold = null)
    {
        try
        {
            var organizationId = GetOrganizationId();
            if (organizationId == Guid.Empty)
                return BadRequest("Organization not found");

            var filter = new ProviderPerformanceFilterDto
            {
                StartDate = startDate ?? DateTime.UtcNow.AddDays(-30),
                EndDate = endDate ?? DateTime.UtcNow,
                IncludeInactive = includeInactive,
                MinItemsThreshold = minItemsThreshold
            };

            var result = await _reportsService.GetProviderPerformanceReportAsync(organizationId, filter);

            if (!result.Success)
                return BadRequest(new { success = false, message = result.Message, errors = result.Errors });

            return Ok(new { success = true, data = result.Data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting provider performance report");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpGet("provider-performance/export")]
    [RequiresTier(SubscriptionTier.Pro)]
    public async Task<IActionResult> ExportProviderPerformanceReport(
        [FromQuery] string format = "csv",
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] bool includeInactive = false,
        [FromQuery] int? minItemsThreshold = null)
    {
        try
        {
            var organizationId = GetOrganizationId();
            if (organizationId == Guid.Empty)
                return BadRequest("Organization not found");

            if (!new[] { "csv", "pdf" }.Contains(format.ToLower()))
                return BadRequest("Unsupported format. Use 'csv' or 'pdf'");

            var filter = new ProviderPerformanceFilterDto
            {
                StartDate = startDate ?? DateTime.UtcNow.AddDays(-30),
                EndDate = endDate ?? DateTime.UtcNow,
                IncludeInactive = includeInactive,
                MinItemsThreshold = minItemsThreshold
            };

            var result = await _reportsService.ExportProviderPerformanceReportAsync(organizationId, filter, format);

            if (!result.Success)
                return BadRequest(new { success = false, message = result.Message, errors = result.Errors });

            var contentType = format.ToLower() == "csv" ? "text/csv" : "application/pdf";
            var fileName = $"provider_performance_{filter.StartDate:yyyy-MM-dd}_{filter.EndDate:yyyy-MM-dd}.{format}";

            return File(result.Data, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting provider performance report");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpGet("inventory-aging")]
    [RequiresTier(SubscriptionTier.Pro)]
    public async Task<IActionResult> GetInventoryAgingReport(
        [FromQuery] int ageThreshold = 30,
        [FromQuery] List<string>? categories = null,
        [FromQuery] List<Guid>? providerIds = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null)
    {
        try
        {
            var organizationId = GetOrganizationId();
            if (organizationId == Guid.Empty)
                return BadRequest("Organization not found");

            var filter = new InventoryAgingFilterDto
            {
                AgeThreshold = ageThreshold,
                Categories = categories,
                ProviderIds = providerIds,
                MinPrice = minPrice,
                MaxPrice = maxPrice
            };

            var result = await _reportsService.GetInventoryAgingReportAsync(organizationId, filter);

            if (!result.Success)
                return BadRequest(new { success = false, message = result.Message, errors = result.Errors });

            return Ok(new { success = true, data = result.Data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inventory aging report");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpGet("inventory-aging/export")]
    [RequiresTier(SubscriptionTier.Pro)]
    public async Task<IActionResult> ExportInventoryAgingReport(
        [FromQuery] string format = "csv",
        [FromQuery] int ageThreshold = 30,
        [FromQuery] List<string>? categories = null,
        [FromQuery] List<Guid>? providerIds = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null)
    {
        try
        {
            var organizationId = GetOrganizationId();
            if (organizationId == Guid.Empty)
                return BadRequest("Organization not found");

            if (!new[] { "csv", "pdf" }.Contains(format.ToLower()))
                return BadRequest("Unsupported format. Use 'csv' or 'pdf'");

            var filter = new InventoryAgingFilterDto
            {
                AgeThreshold = ageThreshold,
                Categories = categories,
                ProviderIds = providerIds,
                MinPrice = minPrice,
                MaxPrice = maxPrice
            };

            var result = await _reportsService.ExportInventoryAgingReportAsync(organizationId, filter, format);

            if (!result.Success)
                return BadRequest(new { success = false, message = result.Message, errors = result.Errors });

            var contentType = format.ToLower() == "csv" ? "text/csv" : "application/pdf";
            var fileName = $"inventory_aging_{ageThreshold}days_{DateTime.UtcNow:yyyy-MM-dd}.{format}";

            return File(result.Data, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting inventory aging report");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpGet("payout-summary")]
    [RequiresTier(SubscriptionTier.Pro)]
    public async Task<IActionResult> GetPayoutSummaryReport(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] List<Guid>? providerIds = null,
        [FromQuery] string? status = null)
    {
        try
        {
            var organizationId = GetOrganizationId();
            if (organizationId == Guid.Empty)
                return BadRequest("Organization not found");

            var filter = new PayoutSummaryFilterDto
            {
                StartDate = startDate ?? DateTime.UtcNow.AddDays(-30),
                EndDate = endDate ?? DateTime.UtcNow,
                ProviderIds = providerIds,
                Status = status
            };

            var result = await _reportsService.GetPayoutSummaryReportAsync(organizationId, filter);

            if (!result.Success)
                return BadRequest(new { success = false, message = result.Message, errors = result.Errors });

            return Ok(new { success = true, data = result.Data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payout summary report");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpGet("payout-summary/export")]
    [RequiresTier(SubscriptionTier.Pro)]
    public async Task<IActionResult> ExportPayoutSummaryReport(
        [FromQuery] string format = "csv",
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] List<Guid>? providerIds = null,
        [FromQuery] string? status = null)
    {
        try
        {
            var organizationId = GetOrganizationId();
            if (organizationId == Guid.Empty)
                return BadRequest("Organization not found");

            if (!new[] { "csv", "pdf" }.Contains(format.ToLower()))
                return BadRequest("Unsupported format. Use 'csv' or 'pdf'");

            var filter = new PayoutSummaryFilterDto
            {
                StartDate = startDate ?? DateTime.UtcNow.AddDays(-30),
                EndDate = endDate ?? DateTime.UtcNow,
                ProviderIds = providerIds,
                Status = status
            };

            var result = await _reportsService.ExportPayoutSummaryReportAsync(organizationId, filter, format);

            if (!result.Success)
                return BadRequest(new { success = false, message = result.Message, errors = result.Errors });

            var contentType = format.ToLower() == "csv" ? "text/csv" : "application/pdf";
            var fileName = $"payout_summary_{filter.StartDate:yyyy-MM-dd}_{filter.EndDate:yyyy-MM-dd}.{format}";

            return File(result.Data, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting payout summary report");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpGet("daily-reconciliation")]
    [RequiresTier(SubscriptionTier.Pro)]
    public async Task<IActionResult> GetDailyReconciliation([FromQuery] DateOnly? date = null)
    {
        try
        {
            var organizationId = GetOrganizationId();
            if (organizationId == Guid.Empty)
                return BadRequest("Organization not found");

            var reportDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var result = await _reportsService.GetDailyReconciliationReportAsync(organizationId, reportDate);

            if (!result.Success)
                return BadRequest(new { success = false, message = result.Message, errors = result.Errors });

            return Ok(new { success = true, data = result.Data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting daily reconciliation report");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpPost("daily-reconciliation")]
    [RequiresTier(SubscriptionTier.Pro)]
    public async Task<IActionResult> SaveDailyReconciliation([FromBody] DailyReconciliationRequestDto request)
    {
        try
        {
            var organizationId = GetOrganizationId();
            if (organizationId == Guid.Empty)
                return BadRequest("Organization not found");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _reportsService.SaveDailyReconciliationAsync(organizationId, request);

            if (!result.Success)
                return BadRequest(new { success = false, message = result.Message, errors = result.Errors });

            return Ok(new { success = true, data = result.Data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving daily reconciliation");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpGet("daily-reconciliation/export")]
    [RequiresTier(SubscriptionTier.Pro)]
    public async Task<IActionResult> ExportDailyReconciliation(
        [FromQuery] string format = "pdf",
        [FromQuery] DateOnly? date = null)
    {
        try
        {
            var organizationId = GetOrganizationId();
            if (organizationId == Guid.Empty)
                return BadRequest("Organization not found");

            if (!new[] { "csv", "pdf" }.Contains(format.ToLower()))
                return BadRequest("Unsupported format. Use 'csv' or 'pdf'");

            var reportDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var result = await _reportsService.ExportDailyReconciliationReportAsync(organizationId, reportDate, format);

            if (!result.Success)
                return BadRequest(new { success = false, message = result.Message, errors = result.Errors });

            var contentType = format.ToLower() == "csv" ? "text/csv" : "application/pdf";
            var fileName = $"daily_reconciliation_{reportDate:yyyy-MM-dd}.{format}";

            return File(result.Data, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting daily reconciliation");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpGet("trends")]
    [RequiresTier(SubscriptionTier.Enterprise)]
    public async Task<IActionResult> GetTrendsReport(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var organizationId = GetOrganizationId();
            if (organizationId == Guid.Empty)
                return BadRequest("Organization not found");

            var filter = new TrendsFilterDto
            {
                StartDate = startDate ?? DateTime.UtcNow.AddDays(-90),
                EndDate = endDate ?? DateTime.UtcNow
            };

            var result = await _reportsService.GetTrendsReportAsync(organizationId, filter);

            if (!result.Success)
                return BadRequest(new { success = false, message = result.Message, errors = result.Errors });

            return Ok(new { success = true, data = result.Data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting trends report");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpGet("inventory-overview")]
    [RequiresTier(SubscriptionTier.Pro)]
    public async Task<IActionResult> GetInventoryOverview()
    {
        try
        {
            var organizationId = GetOrganizationId();
            if (organizationId == Guid.Empty)
                return BadRequest("Organization not found");

            var result = await _reportsService.GetInventoryOverviewAsync(organizationId);

            if (!result.Success)
                return BadRequest(new { success = false, message = result.Message, errors = result.Errors });

            return Ok(new { success = true, data = result.Data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inventory overview");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    private Guid GetOrganizationId()
    {
        var organizationIdClaim = User.FindFirst("OrganizationId")?.Value;
        return Guid.TryParse(organizationIdClaim, out var organizationId) ? organizationId : Guid.Empty;
    }
}