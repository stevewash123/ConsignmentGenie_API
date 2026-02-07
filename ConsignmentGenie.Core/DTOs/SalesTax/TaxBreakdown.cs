using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.DTOs.SalesTax
{
    /// <summary>
    /// Detailed breakdown of tax calculation for a transaction
    /// </summary>
    public class TaxBreakdown
    {
        /// <summary>
        /// Subtotal amount before tax
        /// </summary>
        [Required]
        public decimal Subtotal { get; set; }

        /// <summary>
        /// Tax rate applied (e.g., 0.07 for 7%)
        /// </summary>
        [Required]
        public decimal TaxRate { get; set; }

        /// <summary>
        /// Tax amount calculated (rounded to 2 decimal places)
        /// </summary>
        [Required]
        public decimal TaxAmount { get; set; }

        /// <summary>
        /// Total amount including tax (subtotal + tax amount)
        /// </summary>
        [Required]
        public decimal Total { get; set; }

        /// <summary>
        /// Shop ID used for tax calculation
        /// </summary>
        [Required]
        public Guid ShopId { get; set; }
    }
}