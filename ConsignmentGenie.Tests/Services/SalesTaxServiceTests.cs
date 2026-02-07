using ConsignmentGenie.Application.Services;
using ConsignmentGenie.Core.DTOs.SalesTax;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;
using Moq;
using System.Collections.Generic;

namespace ConsignmentGenie.Tests.Services
{
    public class SalesTaxServiceTests : IDisposable
    {
        private readonly ConsignmentGenieContext _context;
        private readonly Mock<ILogger<MockSalesTaxService>> _mockLogger;
        private readonly Mock<ILogger<ManualSalesTaxService>> _manualLogger;
        private readonly IConfiguration _configuration;

        public SalesTaxServiceTests()
        {
            var options = new DbContextOptionsBuilder<ConsignmentGenieContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ConsignmentGenieContext(options);
            _mockLogger = new Mock<ILogger<MockSalesTaxService>>();
            _manualLogger = new Mock<ILogger<ManualSalesTaxService>>();

            // Use ConfigurationBuilder to create real configuration
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SalesTax:MockTaxRate"] = "0.08"
            });
            _configuration = configurationBuilder.Build();
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public async Task MockSalesTaxService_GetTaxRateAsync_ReturnsConfiguredRate()
        {
            // Arrange
            var service = new MockSalesTaxService(_configuration, _mockLogger.Object);
            var shopId = Guid.NewGuid();

            // Act
            var result = await service.GetTaxRateAsync(shopId);

            // Assert
            Assert.Equal(0.08m, result);
        }

        [Fact]
        public async Task MockSalesTaxService_CalculateTaxAsync_CalculatesCorrectly()
        {
            // Arrange
            var service = new MockSalesTaxService(_configuration, _mockLogger.Object);
            var shopId = Guid.NewGuid();
            var subtotal = 100.00m;

            // Act
            var result = await service.CalculateTaxAsync(shopId, subtotal);

            // Assert
            Assert.Equal(8.00m, result); // 100 * 0.08 = 8.00
        }

        [Fact]
        public async Task MockSalesTaxService_CalculateTaxAsync_RoundsToTwoDecimals()
        {
            // Arrange
            var service = new MockSalesTaxService(_configuration, _mockLogger.Object);
            var shopId = Guid.NewGuid();
            var subtotal = 10.33m; // 10.33 * 0.08 = 0.8264, should round to 0.83

            // Act
            var result = await service.CalculateTaxAsync(shopId, subtotal);

            // Assert
            Assert.Equal(0.83m, result);
        }

        [Fact]
        public async Task MockSalesTaxService_GetTaxBreakdownAsync_ReturnsCorrectBreakdown()
        {
            // Arrange
            var service = new MockSalesTaxService(_configuration, _mockLogger.Object);
            var shopId = Guid.NewGuid();
            var subtotal = 100.00m;

            // Act
            var result = await service.GetTaxBreakdownAsync(shopId, subtotal);

            // Assert
            Assert.Equal(shopId, result.ShopId);
            Assert.Equal(100.00m, result.Subtotal);
            Assert.Equal(0.08m, result.TaxRate);
            Assert.Equal(8.00m, result.TaxAmount);
            Assert.Equal(108.00m, result.Total);
        }

        [Fact]
        public async Task ManualSalesTaxService_GetTaxRateAsync_ReturnsOrganizationRate()
        {
            // Arrange
            var organization = new Organization
            {
                Id = Guid.NewGuid(),
                Name = "Test Shop",
                TaxRate = 0.095m // 9.5%
            };
            await _context.Organizations.AddAsync(organization);
            await _context.SaveChangesAsync();

            var service = new ManualSalesTaxService(_context, _manualLogger.Object);

            // Act
            var result = await service.GetTaxRateAsync(organization.Id);

            // Assert
            Assert.Equal(0.095m, result);
        }

        [Fact]
        public async Task ManualSalesTaxService_GetTaxRateAsync_ReturnsZeroForNonExistentShop()
        {
            // Arrange
            var service = new ManualSalesTaxService(_context, _manualLogger.Object);
            var nonExistentShopId = Guid.NewGuid();

            // Act
            var result = await service.GetTaxRateAsync(nonExistentShopId);

            // Assert
            Assert.Equal(0m, result);
        }

        [Fact]
        public async Task ManualSalesTaxService_CalculateTaxAsync_CalculatesWithOrganizationRate()
        {
            // Arrange
            var organization = new Organization
            {
                Id = Guid.NewGuid(),
                Name = "Test Shop",
                TaxRate = 0.075m // 7.5%
            };
            await _context.Organizations.AddAsync(organization);
            await _context.SaveChangesAsync();

            var service = new ManualSalesTaxService(_context, _manualLogger.Object);
            var subtotal = 200.00m;

            // Act
            var result = await service.CalculateTaxAsync(organization.Id, subtotal);

            // Assert
            Assert.Equal(15.00m, result); // 200 * 0.075 = 15.00
        }

        [Fact]
        public async Task ManualSalesTaxService_GetTaxBreakdownAsync_ReturnsCorrectBreakdown()
        {
            // Arrange
            var organization = new Organization
            {
                Id = Guid.NewGuid(),
                Name = "Test Shop",
                TaxRate = 0.06m // 6%
            };
            await _context.Organizations.AddAsync(organization);
            await _context.SaveChangesAsync();

            var service = new ManualSalesTaxService(_context, _manualLogger.Object);
            var subtotal = 50.00m;

            // Act
            var result = await service.GetTaxBreakdownAsync(organization.Id, subtotal);

            // Assert
            Assert.Equal(organization.Id, result.ShopId);
            Assert.Equal(50.00m, result.Subtotal);
            Assert.Equal(0.06m, result.TaxRate);
            Assert.Equal(3.00m, result.TaxAmount);
            Assert.Equal(53.00m, result.Total);
        }

        [Theory]
        [InlineData(0.0725, 100.00, 7.25)] // 7.25% rate
        [InlineData(0.08875, 45.67, 4.05)] // 8.875% rate, test rounding
        [InlineData(0.00, 100.00, 0.00)] // No tax
        [InlineData(0.10, 0.01, 0.00)] // Very small amount
        public async Task SalesTaxService_TaxCalculation_HandlesVariousRatesAndAmounts(decimal taxRate, decimal subtotal, decimal expectedTax)
        {
            // Arrange
            var organization = new Organization
            {
                Id = Guid.NewGuid(),
                Name = "Test Shop",
                TaxRate = taxRate
            };
            await _context.Organizations.AddAsync(organization);
            await _context.SaveChangesAsync();

            var service = new ManualSalesTaxService(_context, _manualLogger.Object);

            // Act
            var result = await service.CalculateTaxAsync(organization.Id, subtotal);

            // Assert
            Assert.Equal(expectedTax, result);
        }
    }
}