using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using ConsignmentGenie.API.Controllers;
using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Application.DTOs.Reports;
using ConsignmentGenie.Application.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ConsignmentGenie.Tests.Controllers
{
    public class ReportsControllerTests
    {
        private readonly Mock<ISalesReportService> _salesReportServiceMock;
        private readonly Mock<IInventoryReportService> _inventoryReportServiceMock;
        private readonly Mock<IPayoutReportService> _payoutReportServiceMock;
        private readonly Mock<IProviderReportService> _providerReportServiceMock;
        private readonly Mock<IPdfReportGenerator> _pdfReportGeneratorMock;
        private readonly Mock<ICsvExportService> _csvExportServiceMock;
        private readonly Mock<ILogger<ReportsController>> _loggerMock;
        private readonly ReportsController _controller;
        private readonly Guid _organizationId = new("11111111-1111-1111-1111-111111111111");

        public ReportsControllerTests()
        {
            _salesReportServiceMock = new Mock<ISalesReportService>();
            _inventoryReportServiceMock = new Mock<IInventoryReportService>();
            _payoutReportServiceMock = new Mock<IPayoutReportService>();
            _providerReportServiceMock = new Mock<IProviderReportService>();
            _pdfReportGeneratorMock = new Mock<IPdfReportGenerator>();
            _csvExportServiceMock = new Mock<ICsvExportService>();
            _loggerMock = new Mock<ILogger<ReportsController>>();
            _controller = new ReportsController(
                _salesReportServiceMock.Object,
                _inventoryReportServiceMock.Object,
                _payoutReportServiceMock.Object,
                _providerReportServiceMock.Object,
                _pdfReportGeneratorMock.Object,
                _csvExportServiceMock.Object,
                _loggerMock.Object);

            // Setup controller context with organization claim
            var claims = new List<Claim>
            {
                new("OrganizationId", _organizationId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "test");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = principal
                }
            };
        }

        [Fact]
        public async Task GetSalesReport_WithValidRequest_ReturnsOkResult()
        {
            // Arrange
            var salesReportDto = new SalesReportDto
            {
                TotalSales = 1000m,
                ShopRevenue = 400m,
                ProviderPayable = 600m,
                TransactionCount = 10,
                AverageSale = 100m,
                ChartData = new List<SalesChartPointDto>(),
                Transactions = new List<SalesLineItemDto>()
            };

            _salesReportServiceMock
                .Setup(s => s.GetSalesReportAsync(It.IsAny<Guid>(), It.IsAny<SalesReportFilterDto>()))
                .ReturnsAsync(ServiceResult<SalesReportDto>.SuccessResult(salesReportDto));

            // Act
            var result = await _controller.GetSalesReport(
                DateTime.UtcNow.AddDays(-30),
                DateTime.UtcNow,
                null,
                null,
                null);

            // Assert
            var actionResult = Assert.IsType<ActionResult<object>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var response = okResult.Value;
            Assert.NotNull(response);

            // Verify service was called with correct parameters
            _salesReportServiceMock.Verify(
                s => s.GetSalesReportAsync(_organizationId, It.IsAny<SalesReportFilterDto>()),
                Times.Once);
        }

        [Fact]
        public async Task GetSalesReport_WithServiceFailure_ReturnsBadRequest()
        {
            // Arrange
            _salesReportServiceMock
                .Setup(s => s.GetSalesReportAsync(It.IsAny<Guid>(), It.IsAny<SalesReportFilterDto>()))
                .ReturnsAsync(ServiceResult<SalesReportDto>.FailureResult("Service error", new List<string> { "Error details" }));

            // Act
            var result = await _controller.GetSalesReport();

            // Assert
            var actionResult = Assert.IsType<ActionResult<object>>(result);
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
            var response = badRequestResult.Value;
            Assert.NotNull(response);
        }

        [Fact]
        public async Task GetSalesReport_WithoutOrganizationClaim_ReturnsBadRequest()
        {
            // Arrange
            var controllerWithoutClaims = new ReportsController(
                _salesReportServiceMock.Object,
                _inventoryReportServiceMock.Object,
                _payoutReportServiceMock.Object,
                _providerReportServiceMock.Object,
                _pdfReportGeneratorMock.Object,
                _csvExportServiceMock.Object,
                _loggerMock.Object);
            controllerWithoutClaims.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity())
                }
            };

            // Act
            var result = await controllerWithoutClaims.GetSalesReport();

            // Assert
            var actionResult = Assert.IsType<ActionResult<object>>(result);
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
            Assert.Equal("Organization not found", badRequestResult.Value);
        }

        [Fact]
        public async Task ExportSalesReport_CSV_ReturnsFileResult()
        {
            // Arrange
            var csvData = System.Text.Encoding.UTF8.GetBytes("CSV,Content,Here");
            _salesReportServiceMock
                .Setup(s => s.GetSalesReportAsync(It.IsAny<Guid>(), It.IsAny<SalesReportFilterDto>()))
                .ReturnsAsync(ServiceResult<SalesReportDto>.SuccessResult(new SalesReportDto()));

            _csvExportServiceMock
                .Setup(s => s.ExportSalesReportAsync(It.IsAny<SalesReportDto>()))
                .ReturnsAsync(ServiceResult<byte[]>.SuccessResult(csvData));

            // Act
            var result = await _controller.ExportSalesReport("csv");

            // Assert
            var actionResult = Assert.IsType<ActionResult<object>>(result);
        var fileResult = Assert.IsType<FileContentResult>(actionResult.Result);
            Assert.Equal("text/csv", fileResult.ContentType);
            Assert.Contains(".csv", fileResult.FileDownloadName);
            Assert.Equal(csvData, fileResult.FileContents);
        }

        [Fact]
        public async Task ExportSalesReport_UnsupportedFormat_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.ExportSalesReport("xml");

            // Assert
            var actionResult = Assert.IsType<ActionResult<object>>(result);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
            Assert.Equal("Unsupported format. Use 'csv' or 'pdf'", badRequestResult.Value);
        }

        [Fact]
        public async Task GetProviderPerformanceReport_WithValidRequest_ReturnsOkResult()
        {
            // Arrange
            var providerPerformanceDto = new ProviderPerformanceReportDto
            {
                TotalProviders = 5,
                TotalSales = 5000m,
                AverageSalesPerProvider = 1000m,
                TopProviderName = "Top Consignor",
                TopProviderSales = 2000m,
                Consignors = new List<ProviderPerformanceLineDto>()
            };

            _providerReportServiceMock
                .Setup(s => s.GetProviderPerformanceReportAsync(It.IsAny<Guid>(), It.IsAny<ProviderPerformanceFilterDto>()))
                .ReturnsAsync(ServiceResult<ProviderPerformanceReportDto>.SuccessResult(providerPerformanceDto));

            // Act
            var result = await _controller.GetProviderPerformanceReport();

            // Assert
            var actionResult = Assert.IsType<ActionResult<object>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            Assert.NotNull(okResult.Value);

            _providerReportServiceMock.Verify(
                s => s.GetProviderPerformanceReportAsync(_organizationId, It.IsAny<ProviderPerformanceFilterDto>()),
                Times.Once);
        }

        [Fact]
        public async Task GetInventoryAgingReport_WithValidRequest_ReturnsOkResult()
        {
            // Arrange
            var inventoryAgingDto = new InventoryAgingReportDto
            {
                TotalAvailable = 100,
                Over30Days = 20,
                Over60Days = 10,
                Over90Days = 5,
                AverageAge = 45.5,
                AgingBuckets = new List<AgingBucketDto>(),
                Items = new List<AgingItemDto>()
            };

            _inventoryReportServiceMock
                .Setup(s => s.GetInventoryAgingReportAsync(It.IsAny<Guid>(), It.IsAny<InventoryAgingFilterDto>()))
                .ReturnsAsync(ServiceResult<InventoryAgingReportDto>.SuccessResult(inventoryAgingDto));

            // Act
            var result = await _controller.GetInventoryAgingReport(30);

            // Assert
            var actionResult = Assert.IsType<ActionResult<object>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            Assert.NotNull(okResult.Value);

            _inventoryReportServiceMock.Verify(
                s => s.GetInventoryAgingReportAsync(_organizationId, It.Is<InventoryAgingFilterDto>(f => f.AgeThreshold == 30)),
                Times.Once);
        }

        [Fact]
        public async Task GetPayoutSummaryReport_WithValidRequest_ReturnsOkResult()
        {
            // Arrange
            var payoutSummaryDto = new PayoutSummaryReportDto
            {
                TotalPaid = 3000m,
                TotalPending = 1500m,
                ProvidersWithPending = 3,
                AveragePayoutAmount = 500m,
                ChartData = new List<PayoutChartPointDto>(),
                Consignors = new List<PayoutSummaryLineDto>()
            };

            _payoutReportServiceMock
                .Setup(s => s.GetPayoutSummaryReportAsync(It.IsAny<Guid>(), It.IsAny<PayoutSummaryFilterDto>()))
                .ReturnsAsync(ServiceResult<PayoutSummaryReportDto>.SuccessResult(payoutSummaryDto));

            // Act
            var result = await _controller.GetPayoutSummaryReport();

            // Assert
            var actionResult = Assert.IsType<ActionResult<object>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            Assert.NotNull(okResult.Value);

            _payoutReportServiceMock.Verify(
                s => s.GetPayoutSummaryReportAsync(_organizationId, It.IsAny<PayoutSummaryFilterDto>()),
                Times.Once);
        }

        [Fact]
        public async Task GetDailyReconciliation_WithValidRequest_ReturnsOkResult()
        {
            // Arrange
            var reconciliationDto = new DailyReconciliationDto
            {
                Date = DateOnly.FromDateTime(DateTime.UtcNow),
                OpeningBalance = 100m,
                CashSales = 500m,
                CardSales = 300m,
                TotalSales = 800m,
                ExpectedCash = 600m,
                ActualCash = 580m,
                Variance = -20m,
                Transactions = new List<ReconciliationLineDto>()
            };

            _salesReportServiceMock
                .Setup(s => s.GetDailyReconciliationReportAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>()))
                .ReturnsAsync(ServiceResult<DailyReconciliationDto>.SuccessResult(reconciliationDto));

            // Act
            var result = await _controller.GetDailyReconciliation();

            // Assert
            var actionResult = Assert.IsType<ActionResult<object>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            Assert.NotNull(okResult.Value);

            _salesReportServiceMock.Verify(
                s => s.GetDailyReconciliationReportAsync(_organizationId, It.IsAny<DateOnly>()),
                Times.Once);
        }

        [Fact]
        public async Task SaveDailyReconciliation_WithValidRequest_ReturnsOkResult()
        {
            // Arrange
            var request = new DailyReconciliationRequestDto
            {
                Date = DateOnly.FromDateTime(DateTime.UtcNow),
                OpeningBalance = 100m,
                ActualCash = 580m,
                Notes = "Test reconciliation"
            };

            var reconciliationDto = new DailyReconciliationDto
            {
                Date = request.Date,
                OpeningBalance = request.OpeningBalance,
                ActualCash = request.ActualCash,
                Variance = -20m,
                Notes = request.Notes,
                Transactions = new List<ReconciliationLineDto>()
            };

            _salesReportServiceMock
                .Setup(s => s.SaveDailyReconciliationAsync(It.IsAny<Guid>(), It.IsAny<DailyReconciliationRequestDto>()))
                .ReturnsAsync(ServiceResult<DailyReconciliationDto>.SuccessResult(reconciliationDto));

            // Act
            var result = await _controller.SaveDailyReconciliation(request);

            // Assert
            var actionResult = Assert.IsType<ActionResult<object>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            Assert.NotNull(okResult.Value);

            _salesReportServiceMock.Verify(
                s => s.SaveDailyReconciliationAsync(_organizationId, request),
                Times.Once);
        }

        [Fact]
        public async Task GetTrendsReport_WithValidRequest_ReturnsOkResult()
        {
            // Arrange
            var trendsDto = new TrendsReportDto
            {
                WeeklyTrends = new List<WeeklyTrendDto>(),
                CategoryTrends = new List<CategoryTrendDto>(),
                Summary = new TrendsSummaryDto
                {
                    TotalPeriods = 12,
                    AverageWeeklyRevenue = 1500m,
                    GrowthRate = 5.5m,
                    TopCategory = "Electronics"
                }
            };

            _salesReportServiceMock
                .Setup(s => s.GetTrendsReportAsync(It.IsAny<Guid>(), It.IsAny<TrendsFilterDto>()))
                .ReturnsAsync(ServiceResult<TrendsReportDto>.SuccessResult(trendsDto));

            // Act
            var result = await _controller.GetTrendsReport();

            // Assert
            var actionResult = Assert.IsType<ActionResult<object>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            Assert.NotNull(okResult.Value);

            _salesReportServiceMock.Verify(
                s => s.GetTrendsReportAsync(_organizationId, It.IsAny<TrendsFilterDto>()),
                Times.Once);
        }

        [Fact]
        public async Task GetInventoryOverview_WithValidRequest_ReturnsOkResult()
        {
            // Arrange
            var inventoryOverviewDto = new InventoryOverviewDto
            {
                TotalItems = 250,
                AvailableItems = 180,
                SoldItems = 70,
                AveragePrice = 85.50m,
                TotalInventoryValue = 15390m,
                CategoryBreakdown = new List<CategoryBreakdownDto>(),
                ProviderBreakdown = new List<ProviderBreakdownDto>()
            };

            _inventoryReportServiceMock
                .Setup(s => s.GetInventoryOverviewAsync(It.IsAny<Guid>()))
                .ReturnsAsync(ServiceResult<InventoryOverviewDto>.SuccessResult(inventoryOverviewDto));

            // Act
            var result = await _controller.GetInventoryOverview();

            // Assert
            var actionResult = Assert.IsType<ActionResult<object>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            Assert.NotNull(okResult.Value);

            _inventoryReportServiceMock.Verify(
                s => s.GetInventoryOverviewAsync(_organizationId),
                Times.Once);
        }

        [Fact]
        public async Task ExportProviderPerformanceReport_PDF_ReturnsFileResult()
        {
            // Arrange
            var pdfData = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // PDF header bytes
            _providerReportServiceMock
                .Setup(s => s.GetProviderPerformanceReportAsync(It.IsAny<Guid>(), It.IsAny<ProviderPerformanceFilterDto>()))
                .ReturnsAsync(ServiceResult<ProviderPerformanceReportDto>.SuccessResult(new ProviderPerformanceReportDto()));

            _pdfReportGeneratorMock
                .Setup(s => s.GenerateProviderPerformanceReportPdfAsync(It.IsAny<ProviderPerformanceReportDto>(), It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<byte[]>.SuccessResult(pdfData));

            // Act
            var result = await _controller.ExportProviderPerformanceReport("pdf");

            // Assert
            var actionResult = Assert.IsType<ActionResult<object>>(result);
        var fileResult = Assert.IsType<FileContentResult>(actionResult.Result);
            Assert.Equal("application/pdf", fileResult.ContentType);
            Assert.Contains(".pdf", fileResult.FileDownloadName);
            Assert.Equal(pdfData, fileResult.FileContents);
        }

        [Fact]
        public async Task ExportInventoryAgingReport_WithExportFailure_ReturnsBadRequest()
        {
            // Arrange
            _inventoryReportServiceMock
                .Setup(s => s.GetInventoryAgingReportAsync(It.IsAny<Guid>(), It.IsAny<InventoryAgingFilterDto>()))
                .ReturnsAsync(ServiceResult<InventoryAgingReportDto>.SuccessResult(new InventoryAgingReportDto()));

            _csvExportServiceMock
                .Setup(s => s.ExportInventoryAgingReportAsync(It.IsAny<InventoryAgingReportDto>()))
                .ReturnsAsync(ServiceResult<byte[]>.FailureResult("Export failed", new List<string> { "Internal error" }));

            // Act
            var result = await _controller.ExportInventoryAgingReport("csv");

            // Assert
            var actionResult = Assert.IsType<ActionResult<object>>(result);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
            var response = badRequestResult.Value;
            Assert.NotNull(response);
        }

        [Fact]
        public async Task GetSalesReport_WithFilters_PassesCorrectFilters()
        {
            // Arrange
            var startDate = DateTime.UtcNow.AddDays(-30);
            var endDate = DateTime.UtcNow;
            var providerIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
            var categories = new List<string> { "Electronics", "Clothing" };
            var paymentMethods = new List<string> { "Cash", "Card" };

            _salesReportServiceMock
                .Setup(s => s.GetSalesReportAsync(It.IsAny<Guid>(), It.IsAny<SalesReportFilterDto>()))
                .ReturnsAsync(ServiceResult<SalesReportDto>.SuccessResult(new SalesReportDto()));

            // Act
            var result = await _controller.GetSalesReport(startDate, endDate, providerIds, categories, paymentMethods);

            // Assert
            var actionResult = Assert.IsType<ActionResult<object>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);

            _salesReportServiceMock.Verify(
                s => s.GetSalesReportAsync(_organizationId, It.Is<SalesReportFilterDto>(f =>
                    f.StartDate == startDate &&
                    f.EndDate == endDate &&
                    f.ConsignorIds == providerIds &&
                    f.Categories == categories &&
                    f.PaymentMethods == paymentMethods)),
                Times.Once);
        }

        [Fact]
        public async Task GetProviderPerformanceReport_WithParameters_PassesCorrectFilter()
        {
            // Arrange
            var startDate = DateTime.UtcNow.AddDays(-60);
            var endDate = DateTime.UtcNow.AddDays(-1);
            var includeInactive = true;
            var minItemsThreshold = 5;

            _providerReportServiceMock
                .Setup(s => s.GetProviderPerformanceReportAsync(It.IsAny<Guid>(), It.IsAny<ProviderPerformanceFilterDto>()))
                .ReturnsAsync(ServiceResult<ProviderPerformanceReportDto>.SuccessResult(new ProviderPerformanceReportDto()));

            // Act
            var result = await _controller.GetProviderPerformanceReport(startDate, endDate, includeInactive, minItemsThreshold);

            // Assert
            var actionResult = Assert.IsType<ActionResult<object>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);

            _providerReportServiceMock.Verify(
                s => s.GetProviderPerformanceReportAsync(_organizationId, It.Is<ProviderPerformanceFilterDto>(f =>
                    f.StartDate == startDate &&
                    f.EndDate == endDate &&
                    f.IncludeInactive == includeInactive &&
                    f.MinItemsThreshold == minItemsThreshold)),
                Times.Once);
        }

        [Fact]
        public async Task GetInventoryAgingReport_WithAllFilters_PassesCorrectFilter()
        {
            // Arrange
            var ageThreshold = 60;
            var categories = new List<string> { "Books", "Art" };
            var providerIds = new List<Guid> { Guid.NewGuid() };
            var minPrice = 10m;
            var maxPrice = 500m;

            _inventoryReportServiceMock
                .Setup(s => s.GetInventoryAgingReportAsync(It.IsAny<Guid>(), It.IsAny<InventoryAgingFilterDto>()))
                .ReturnsAsync(ServiceResult<InventoryAgingReportDto>.SuccessResult(new InventoryAgingReportDto()));

            // Act
            var result = await _controller.GetInventoryAgingReport(ageThreshold, categories, providerIds, minPrice, maxPrice);

            // Assert
            var actionResult = Assert.IsType<ActionResult<object>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);

            _inventoryReportServiceMock.Verify(
                s => s.GetInventoryAgingReportAsync(_organizationId, It.Is<InventoryAgingFilterDto>(f =>
                    f.AgeThreshold == ageThreshold &&
                    f.Categories == categories &&
                    f.ConsignorIds == providerIds &&
                    f.MinPrice == minPrice &&
                    f.MaxPrice == maxPrice)),
                Times.Once);
        }
    }
}