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
    private readonly ISalesReportService _salesReportService;
    private readonly IInventoryReportService _inventoryReportService;
    private readonly IPayoutReportService _payoutReportService;
    private readonly IConsignorReportService _consignorReportService;
    private readonly IPdfReportGenerator _pdfReportGenerator;
    private readonly ICsvExportService _csvExportService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        ISalesReportService salesReportService,
        IInventoryReportService inventoryReportService,
        IPayoutReportService payoutReportService,
        IConsignorReportService consignorReportService,
        IPdfReportGenerator pdfReportGenerator,
        ICsvExportService csvExportService,
        ILogger<ReportsController> logger)
    {
        _salesReportService = salesReportService;
        _inventoryReportService = inventoryReportService;
        _payoutReportService = payoutReportService;
        _consignorReportService = consignorReportService;
        _pdfReportGenerator = pdfReportGenerator;
        _csvExportService = csvExportService;
        _logger = logger;
    }

    [HttpGet("sales")]
    [RequiresTier(SubscriptionTier.Pro)]
    public async Task<ActionResult<object>> GetSalesReport(
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
                ConsignorIds = providerIds,
                Categories = categories,
                PaymentMethods = paymentMethods
            };

            var result = await _salesReportService.GetSalesReportAsync(organizationId, filter);

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
    public async Task<ActionResult<object>> ExportSalesReport(
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
                ConsignorIds = providerIds,
                Categories = categories,
                PaymentMethods = paymentMethods
            };

            // Get the sales report data
            var reportResult = await _salesReportService.GetSalesReportAsync(organizationId, filter);
            if (!reportResult.Success)
                return BadRequest(new { success = false, message = reportResult.Message, errors = reportResult.Errors });

            // Export based on format
            var result = format.ToLower() == "csv"
                ? await _csvExportService.ExportSalesReportAsync(reportResult.Data)
                : await _pdfReportGenerator.GenerateSalesReportPdfAsync(reportResult.Data, $"Sales Report ({filter.StartDate:yyyy-MM-dd} to {filter.EndDate:yyyy-MM-dd})");

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

    [HttpGet("consignor-performance")]
    [RequiresTier(SubscriptionTier.Pro)]
    public async Task<ActionResult<object>> GetConsignorPerformanceReport(
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

            var filter = new ConsignorPerformanceFilterDto
            {
                StartDate = startDate ?? DateTime.UtcNow.AddDays(-30),
                EndDate = endDate ?? DateTime.UtcNow,
                IncludeInactive = includeInactive,
                MinItemsThreshold = minItemsThreshold
            };

            var result = await _consignorReportService.GetConsignorPerformanceReportAsync(organizationId, filter);

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

    [HttpGet("consignor-performance/export")]
    [RequiresTier(SubscriptionTier.Pro)]
    public async Task<ActionResult<object>> ExportConsignorPerformanceReport(
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

            var filter = new ConsignorPerformanceFilterDto
            {
                StartDate = startDate ?? DateTime.UtcNow.AddDays(-30),
                EndDate = endDate ?? DateTime.UtcNow,
                IncludeInactive = includeInactive,
                MinItemsThreshold = minItemsThreshold
            };

            // Get the provider performance report data
            var reportResult = await _consignorReportService.GetConsignorPerformanceReportAsync(organizationId, filter);
            if (!reportResult.Success)
                return BadRequest(new { success = false, message = reportResult.Message, errors = reportResult.Errors });

            // Export based on format
            var result = format.ToLower() == "csv"
                ? await _csvExportService.ExportConsignorPerformanceReportAsync(reportResult.Data)
                : await _pdfReportGenerator.GenerateConsignorPerformanceReportPdfAsync(reportResult.Data, $"Consignor Performance Report ({filter.StartDate:yyyy-MM-dd} to {filter.EndDate:yyyy-MM-dd})");

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
    public async Task<ActionResult<object>> GetInventoryAgingReport(
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
                ConsignorIds = providerIds,
                MinPrice = minPrice,
                MaxPrice = maxPrice
            };

            var result = await _inventoryReportService.GetInventoryAgingReportAsync(organizationId, filter);

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
    public async Task<ActionResult<object>> ExportInventoryAgingReport(
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
                ConsignorIds = providerIds,
                MinPrice = minPrice,
                MaxPrice = maxPrice
            };

            // Get the inventory aging report data
            var reportResult = await _inventoryReportService.GetInventoryAgingReportAsync(organizationId, filter);
            if (!reportResult.Success)
                return BadRequest(new { success = false, message = reportResult.Message, errors = reportResult.Errors });

            // Export based on format
            var result = format.ToLower() == "csv"
                ? await _csvExportService.ExportInventoryAgingReportAsync(reportResult.Data)
                : await _pdfReportGenerator.GenerateInventoryAgingReportPdfAsync(reportResult.Data, $"Inventory Aging Report ({ageThreshold} days threshold)");

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
    public async Task<ActionResult<object>> GetPayoutSummaryReport(
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
                ConsignorIds = providerIds,
                Status = status
            };

            var result = await _payoutReportService.GetPayoutSummaryReportAsync(organizationId, filter);

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
    public async Task<ActionResult<object>> ExportPayoutSummaryReport(
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
                ConsignorIds = providerIds,
                Status = status
            };

            // Get the payout summary report data
            var reportResult = await _payoutReportService.GetPayoutSummaryReportAsync(organizationId, filter);
            if (!reportResult.Success)
                return BadRequest(new { success = false, message = reportResult.Message, errors = reportResult.Errors });

            // Export based on format
            var result = format.ToLower() == "csv"
                ? await _csvExportService.ExportPayoutSummaryReportAsync(reportResult.Data)
                : await _pdfReportGenerator.GeneratePayoutSummaryReportPdfAsync(reportResult.Data, $"Payout Summary Report ({filter.StartDate:yyyy-MM-dd} to {filter.EndDate:yyyy-MM-dd})");

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
    public async Task<ActionResult<object>> GetDailyReconciliation([FromQuery] DateOnly? date = null)
    {
        try
        {
            var organizationId = GetOrganizationId();
            if (organizationId == Guid.Empty)
                return BadRequest("Organization not found");

            var reportDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var result = await _salesReportService.GetDailyReconciliationReportAsync(organizationId, reportDate);

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
    public async Task<ActionResult<object>> SaveDailyReconciliation([FromBody] DailyReconciliationRequestDto request)
    {
        try
        {
            var organizationId = GetOrganizationId();
            if (organizationId == Guid.Empty)
                return BadRequest("Organization not found");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _salesReportService.SaveDailyReconciliationAsync(organizationId, request);

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
    public async Task<ActionResult<object>> ExportDailyReconciliation(
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
            // Get the daily reconciliation report data
            var reportResult = await _salesReportService.GetDailyReconciliationReportAsync(organizationId, reportDate);
            if (!reportResult.Success)
                return BadRequest(new { success = false, message = reportResult.Message, errors = reportResult.Errors });

            // Export based on format
            var result = format.ToLower() == "csv"
                ? await _csvExportService.ExportDailyReconciliationReportAsync(reportResult.Data)
                : await _pdfReportGenerator.GenerateDailyReconciliationReportPdfAsync(reportResult.Data, $"Daily Reconciliation Report ({reportDate:yyyy-MM-dd})");

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
    public async Task<ActionResult<object>> GetTrendsReport(
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

            var result = await _salesReportService.GetTrendsReportAsync(organizationId, filter);

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
    public async Task<ActionResult<object>> GetInventoryOverview()
    {
        try
        {
            var organizationId = GetOrganizationId();
            if (organizationId == Guid.Empty)
                return BadRequest("Organization not found");

            var result = await _inventoryReportService.GetInventoryOverviewAsync(organizationId);

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