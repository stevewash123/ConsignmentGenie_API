using ConsignmentGenie.Core.DTOs.SalesTax;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ConsignmentGenie.Application.Services
{
    /// <summary>
    /// Manual implementation of sales tax service that reads tax rate from organization settings
    /// </summary>
    public class ManualSalesTaxService : ISalesTaxService
    {
        private readonly ConsignmentGenieContext _context;
        private readonly ILogger<ManualSalesTaxService> _logger;

        public ManualSalesTaxService(ConsignmentGenieContext context, ILogger<ManualSalesTaxService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<decimal> GetTaxRateAsync(Guid shopId)
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == shopId);

            if (organization == null)
            {
                _logger.LogWarning("Organization {ShopId} not found, returning 0% tax rate", shopId);
                return 0m;
            }

            _logger.LogDebug("Retrieved tax rate {Rate} for organization {ShopId}", organization.TaxRate, shopId);
            return organization.TaxRate;
        }

        public async Task<decimal> CalculateTaxAsync(Guid shopId, decimal subtotal)
        {
            var taxRate = await GetTaxRateAsync(shopId);
            var taxAmount = Math.Round(subtotal * taxRate, 2, MidpointRounding.AwayFromZero);

            _logger.LogDebug("ManualSalesTaxService calculated tax {TaxAmount} for subtotal {Subtotal} at rate {Rate} for shop {ShopId}",
                taxAmount, subtotal, taxRate, shopId);

            return taxAmount;
        }

        public async Task<TaxBreakdown> GetTaxBreakdownAsync(Guid shopId, decimal subtotal)
        {
            var taxRate = await GetTaxRateAsync(shopId);
            var taxAmount = Math.Round(subtotal * taxRate, 2, MidpointRounding.AwayFromZero);
            var total = subtotal + taxAmount;

            var breakdown = new TaxBreakdown
            {
                ShopId = shopId,
                Subtotal = subtotal,
                TaxRate = taxRate,
                TaxAmount = taxAmount,
                Total = total
            };

            _logger.LogDebug("ManualSalesTaxService created breakdown for shop {ShopId}: Subtotal={Subtotal}, Tax={TaxAmount}, Total={Total}",
                shopId, subtotal, taxAmount, total);

            return breakdown;
        }
    }
}