using ConsignmentGenie.Core.DTOs.SalesTax;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ConsignmentGenie.Application.Services
{
    /// <summary>
    /// Mock implementation of sales tax service for development and testing
    /// Returns configurable fixed rate from app settings
    /// </summary>
    public class MockSalesTaxService : ISalesTaxService
    {
        private readonly ILogger<MockSalesTaxService> _logger;
        private readonly decimal _mockTaxRate;

        public MockSalesTaxService(IConfiguration configuration, ILogger<MockSalesTaxService> logger)
        {
            _logger = logger;
            _mockTaxRate = configuration.GetValue<decimal>("SalesTax:MockTaxRate", 0.07m);
            _logger.LogInformation("MockSalesTaxService initialized with rate: {Rate}", _mockTaxRate);
        }

        public async Task<decimal> GetTaxRateAsync(Guid shopId)
        {
            await Task.CompletedTask; // Simulate async operation
            _logger.LogDebug("MockSalesTaxService returning fixed rate {Rate} for shop {ShopId}", _mockTaxRate, shopId);
            return _mockTaxRate;
        }

        public async Task<decimal> CalculateTaxAsync(Guid shopId, decimal subtotal)
        {
            await Task.CompletedTask; // Simulate async operation
            var taxAmount = Math.Round(subtotal * _mockTaxRate, 2, MidpointRounding.AwayFromZero);
            _logger.LogDebug("MockSalesTaxService calculated tax {TaxAmount} for subtotal {Subtotal} at rate {Rate}",
                taxAmount, subtotal, _mockTaxRate);
            return taxAmount;
        }

        public async Task<TaxBreakdown> GetTaxBreakdownAsync(Guid shopId, decimal subtotal)
        {
            await Task.CompletedTask; // Simulate async operation

            var taxAmount = Math.Round(subtotal * _mockTaxRate, 2, MidpointRounding.AwayFromZero);
            var total = subtotal + taxAmount;

            var breakdown = new TaxBreakdown
            {
                ShopId = shopId,
                Subtotal = subtotal,
                TaxRate = _mockTaxRate,
                TaxAmount = taxAmount,
                Total = total
            };

            _logger.LogDebug("MockSalesTaxService created breakdown for shop {ShopId}: Subtotal={Subtotal}, Tax={TaxAmount}, Total={Total}",
                shopId, subtotal, taxAmount, total);

            return breakdown;
        }
    }
}