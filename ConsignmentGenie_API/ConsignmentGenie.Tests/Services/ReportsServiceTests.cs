using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ConsignmentGenie.Application.DTOs.Reports;
using ConsignmentGenie.Application.Services;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Core.Interfaces;
using ConsignmentGenie.Infrastructure.Data;
using ConsignmentGenie.Infrastructure.Repositories;
using ConsignmentGenie.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ConsignmentGenie.Tests.Services
{
    public class ReportsServiceTests : IDisposable
    {
        private readonly ConsignmentGenieContext _context;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ReportsService _reportsService;
        private readonly Mock<ILogger<ReportsService>> _loggerMock;

        private readonly Guid _organizationId = new("11111111-1111-1111-1111-111111111111");
        private readonly Guid _providerId1 = new("22222222-2222-2222-2222-222222222222");
        private readonly Guid _providerId2 = new("33333333-3333-3333-3333-333333333333");

        public ReportsServiceTests()
        {
            _context = TestDbContextFactory.CreateInMemoryContext();
            _unitOfWork = new UnitOfWork(_context);
            _loggerMock = new Mock<ILogger<ReportsService>>();
            _reportsService = new ReportsService(_unitOfWork, _loggerMock.Object);

            SeedTestData().Wait();
        }

        private async Task SeedTestData()
        {
            // Add organization
            var organization = new Organization
            {
                Id = _organizationId,
                Name = "Test Shop",
                VerticalType = VerticalType.Consignment,
                SubscriptionStatus = SubscriptionStatus.Active,
                SubscriptionTier = SubscriptionTier.Pro,
                CreatedAt = DateTime.UtcNow.AddMonths(-1),
                UpdatedAt = DateTime.UtcNow
            };
            _context.Organizations.Add(organization);

            // Add providers
            var provider1 = new Provider
            {
                Id = _providerId1,
                OrganizationId = _organizationId,
                DisplayName = "Provider One",
                Email = "provider1@test.com",
                DefaultSplitPercentage = 60m,
                Status = ProviderStatus.Active,
                CreatedAt = DateTime.UtcNow.AddMonths(-1)
            };

            var provider2 = new Provider
            {
                Id = _providerId2,
                OrganizationId = _organizationId,
                DisplayName = "Provider Two",
                Email = "provider2@test.com",
                DefaultSplitPercentage = 70m,
                Status = ProviderStatus.Active,
                CreatedAt = DateTime.UtcNow.AddMonths(-1)
            };

            _context.Providers.AddRange(provider1, provider2);

            // Add items
            var item1 = new Item
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                ProviderId = _providerId1,
                Sku = "SKU001",
                Title = "Test Item 1",
                Category = "Electronics",
                Price = 100m,
                Status = ItemStatus.Sold,
                ListedDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
                SoldDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)),
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            };

            var item2 = new Item
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                ProviderId = _providerId2,
                Sku = "SKU002",
                Title = "Test Item 2",
                Category = "Clothing",
                Price = 50m,
                Status = ItemStatus.Available,
                ListedDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-60)),
                CreatedAt = DateTime.UtcNow.AddDays(-60)
            };

            var item3 = new Item
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                ProviderId = _providerId1,
                Sku = "SKU003",
                Title = "Test Item 3",
                Category = "Electronics",
                Price = 150m,
                Status = ItemStatus.Available,
                ListedDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)),
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            };

            _context.Items.AddRange(item1, item2, item3);

            // Add transactions
            var transaction1 = new Transaction
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                ItemId = item1.Id,
                ProviderId = _providerId1,
                SalePrice = 100m,
                ProviderSplitPercentage = 60m,
                ProviderAmount = 60m,
                ShopAmount = 40m,
                SaleDate = DateTime.UtcNow.AddDays(-5),
                PaymentMethod = "Cash",
                PayoutStatus = "Pending",
                Status = "Completed"
            };

            _context.Transactions.Add(transaction1);

            // Add payouts
            var payout1 = new Payout
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                ProviderId = _providerId1,
                PayoutNumber = "PAY001",
                PayoutDate = DateTime.UtcNow.AddDays(-2),
                Amount = 60m,
                Status = PayoutStatus.Paid,
                PaymentMethod = "Check",
                PeriodStart = DateTime.UtcNow.AddDays(-30),
                PeriodEnd = DateTime.UtcNow.AddDays(-1),
                TransactionCount = 1
            };

            _context.Payouts.Add(payout1);

            await _context.SaveChangesAsync();
        }

        [Fact]
        public async Task GetSalesReportAsync_ReturnsCorrectData()
        {
            // Arrange
            var filter = new SalesReportFilterDto
            {
                StartDate = DateTime.UtcNow.AddDays(-30),
                EndDate = DateTime.UtcNow
            };

            // Act
            var result = await _reportsService.GetSalesReportAsync(_organizationId, filter);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(100m, result.Data.TotalSales);
            Assert.Equal(40m, result.Data.ShopRevenue);
            Assert.Equal(60m, result.Data.ProviderPayable);
            Assert.Equal(1, result.Data.TransactionCount);
            Assert.Equal(100m, result.Data.AverageSale);
            Assert.Single(result.Data.Transactions);
        }

        [Fact]
        public async Task GetSalesReportAsync_WithProviderFilter_FiltersCorrectly()
        {
            // Arrange
            var filter = new SalesReportFilterDto
            {
                StartDate = DateTime.UtcNow.AddDays(-30),
                EndDate = DateTime.UtcNow,
                ProviderIds = new List<Guid> { _providerId1 }
            };

            // Act
            var result = await _reportsService.GetSalesReportAsync(_organizationId, filter);

            // Assert
            Assert.True(result.Success);
            Assert.Single(result.Data.Transactions);
            Assert.Equal("Provider One", result.Data.Transactions.First().ProviderName);
        }

        [Fact]
        public async Task GetProviderPerformanceReportAsync_ReturnsCorrectData()
        {
            // Arrange
            var filter = new ProviderPerformanceFilterDto
            {
                StartDate = DateTime.UtcNow.AddDays(-30),
                EndDate = DateTime.UtcNow,
                IncludeInactive = false
            };

            // Act
            var result = await _reportsService.GetProviderPerformanceReportAsync(_organizationId, filter);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(2, result.Data.TotalProviders);
            Assert.Equal(100m, result.Data.TotalSales);
            Assert.Equal(50m, result.Data.AverageSalesPerProvider);
            Assert.Equal("Provider One", result.Data.TopProviderName);
            Assert.Equal(100m, result.Data.TopProviderSales);
            Assert.Equal(2, result.Data.Providers.Count);
        }

        [Fact]
        public async Task GetInventoryAgingReportAsync_ReturnsCorrectData()
        {
            // Arrange
            var filter = new InventoryAgingFilterDto
            {
                AgeThreshold = 30
            };

            // Act
            var result = await _reportsService.GetInventoryAgingReportAsync(_organizationId, filter);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(2, result.Data.TotalAvailable); // 2 available items
            Assert.Single(result.Data.Items); // Only 1 item over 30 days old
            Assert.Equal("Test Item 2", result.Data.Items.First().Name);
            Assert.True(result.Data.Items.First().DaysListed >= 30);
        }

        [Fact]
        public async Task GetPayoutSummaryReportAsync_ReturnsCorrectData()
        {
            // Arrange
            var filter = new PayoutSummaryFilterDto
            {
                StartDate = DateTime.UtcNow.AddDays(-30),
                EndDate = DateTime.UtcNow
            };

            // Clear EF tracking state
            _context.ChangeTracker.Clear();

            // Act
            var result = await _reportsService.GetPayoutSummaryReportAsync(_organizationId, filter);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(60m, result.Data.TotalPaid);
            Assert.Equal(60m, result.Data.TotalPending); // Same transaction showing as pending in this period
            Assert.Equal(1, result.Data.ProvidersWithPending);
            Assert.Equal(60m, result.Data.AveragePayoutAmount);
            Assert.Single(result.Data.Providers);
        }

        [Fact]
        public async Task GetDailyReconciliationReportAsync_ReturnsCorrectData()
        {
            // Arrange
            var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5));

            // Act
            var result = await _reportsService.GetDailyReconciliationReportAsync(_organizationId, date);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(date, result.Data.Date);
            Assert.Equal(100m, result.Data.CashSales);
            Assert.Equal(0m, result.Data.CardSales);
            Assert.Equal(100m, result.Data.TotalSales);
            Assert.Single(result.Data.Transactions);
        }

        [Fact]
        public async Task SaveDailyReconciliationAsync_CalculatesVarianceCorrectly()
        {
            // Arrange
            var request = new DailyReconciliationRequestDto
            {
                Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)),
                OpeningBalance = 50m,
                ActualCash = 140m,
                Notes = "Test reconciliation"
            };

            // Act
            var result = await _reportsService.SaveDailyReconciliationAsync(_organizationId, request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(50m, result.Data.OpeningBalance);
            Assert.Equal(140m, result.Data.ActualCash);
            Assert.Equal(150m, result.Data.ExpectedCash); // Opening + Cash Sales
            Assert.Equal(-10m, result.Data.Variance); // Actual - Expected
            Assert.Equal("Test reconciliation", result.Data.Notes);
        }

        [Fact]
        public async Task GetTrendsReportAsync_ReturnsCorrectData()
        {
            // Arrange
            var filter = new TrendsFilterDto
            {
                StartDate = DateTime.UtcNow.AddDays(-90),
                EndDate = DateTime.UtcNow
            };

            // Act
            var result = await _reportsService.GetTrendsReportAsync(_organizationId, filter);

            // Assert
            Assert.True(result.Success);
            Assert.Single(result.Data.WeeklyTrends); // Should have one week of data
            Assert.Single(result.Data.CategoryTrends); // Should have Electronics category
            Assert.Equal("Electronics", result.Data.CategoryTrends.First().Category);
            Assert.Equal(100m, result.Data.CategoryTrends.First().TotalRevenue);
            Assert.NotNull(result.Data.Summary);
        }

        [Fact]
        public async Task GetInventoryOverviewAsync_ReturnsCorrectData()
        {
            // Act
            var result = await _reportsService.GetInventoryOverviewAsync(_organizationId);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(3, result.Data.TotalItems);
            Assert.Equal(2, result.Data.AvailableItems);
            Assert.Equal(1, result.Data.SoldItems);
            Assert.Equal(200m, result.Data.TotalInventoryValue); // 50 + 150
            Assert.Equal(2, result.Data.CategoryBreakdown.Count);
            Assert.Equal(2, result.Data.ProviderBreakdown.Count);
        }

        [Fact]
        public async Task ExportSalesReportAsync_CSV_ReturnsValidContent()
        {
            // Arrange
            var filter = new SalesReportFilterDto
            {
                StartDate = DateTime.UtcNow.AddDays(-30),
                EndDate = DateTime.UtcNow
            };

            // Act
            var result = await _reportsService.ExportSalesReportAsync(_organizationId, filter, "csv");

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);

            var csvContent = System.Text.Encoding.UTF8.GetString(result.Data);
            Assert.Contains("Date,Item Name,Category,Provider Name", csvContent);
            Assert.Contains("Test Item 1", csvContent);
            Assert.Contains("Provider One", csvContent);
        }

        [Fact]
        public async Task ExportSalesReportAsync_PDF_ReturnsValidContent()
        {
            // Arrange
            var filter = new SalesReportFilterDto
            {
                StartDate = DateTime.UtcNow.AddDays(-30),
                EndDate = DateTime.UtcNow
            };

            // Act
            var result = await _reportsService.ExportSalesReportAsync(_organizationId, filter, "pdf");

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.True(result.Data.Length > 0);

            // PDF content should start with PDF header
            var pdfHeader = System.Text.Encoding.ASCII.GetString(result.Data.Take(4).ToArray());
            Assert.Equal("%PDF", pdfHeader);
        }

        [Fact]
        public async Task ExportSalesReportAsync_UnsupportedFormat_ReturnsError()
        {
            // Arrange
            var filter = new SalesReportFilterDto
            {
                StartDate = DateTime.UtcNow.AddDays(-30),
                EndDate = DateTime.UtcNow
            };

            // Act
            var result = await _reportsService.ExportSalesReportAsync(_organizationId, filter, "xml");

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Unsupported format", result.Message);
        }

        [Fact]
        public async Task GetSalesReportAsync_WithNonExistentOrganization_ReturnsEmptyData()
        {
            // Arrange
            var nonExistentOrgId = Guid.NewGuid();
            var filter = new SalesReportFilterDto
            {
                StartDate = DateTime.UtcNow.AddDays(-30),
                EndDate = DateTime.UtcNow
            };

            // Act
            var result = await _reportsService.GetSalesReportAsync(nonExistentOrgId, filter);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(0m, result.Data.TotalSales);
            Assert.Empty(result.Data.Transactions);
        }

        [Fact]
        public async Task GetProviderPerformanceReportAsync_WithMinItemsThreshold_FiltersCorrectly()
        {
            // Arrange
            var filter = new ProviderPerformanceFilterDto
            {
                StartDate = DateTime.UtcNow.AddDays(-30),
                EndDate = DateTime.UtcNow,
                MinItemsThreshold = 3 // Should filter out providers with fewer items
            };

            // Act
            var result = await _reportsService.GetProviderPerformanceReportAsync(_organizationId, filter);

            // Assert
            Assert.True(result.Success);
            Assert.Empty(result.Data.Providers); // No provider has 3+ items
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}