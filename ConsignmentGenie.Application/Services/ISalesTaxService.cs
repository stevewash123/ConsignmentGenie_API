using ConsignmentGenie.Core.DTOs.SalesTax;

namespace ConsignmentGenie.Application.Services
{
    /// <summary>
    /// Service interface for calculating sales tax with pluggable implementations
    /// </summary>
    public interface ISalesTaxService
    {
        /// <summary>
        /// Gets the tax rate for a specific shop
        /// </summary>
        /// <param name="shopId">Shop identifier</param>
        /// <returns>Tax rate as decimal (e.g., 0.07 for 7%)</returns>
        Task<decimal> GetTaxRateAsync(Guid shopId);

        /// <summary>
        /// Calculates tax amount for a given subtotal
        /// </summary>
        /// <param name="shopId">Shop identifier</param>
        /// <param name="subtotal">Subtotal before tax</param>
        /// <returns>Tax amount calculated to 2 decimal places</returns>
        Task<decimal> CalculateTaxAsync(Guid shopId, decimal subtotal);

        /// <summary>
        /// Gets a complete tax breakdown including subtotal, rate, tax amount, and total
        /// </summary>
        /// <param name="shopId">Shop identifier</param>
        /// <param name="subtotal">Subtotal before tax</param>
        /// <returns>Complete tax breakdown</returns>
        Task<TaxBreakdown> GetTaxBreakdownAsync(Guid shopId, decimal subtotal);
    }
}