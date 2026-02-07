using ConsignmentGenie.Core.Entities;

namespace ConsignmentGenie.Core.Extensions
{
    /// <summary>
    /// Temporary compatibility extensions to ease transition from old Transaction model to new TransactionItem pattern.
    /// These should be removed once all controllers are properly updated.
    /// </summary>
    public static class TransactionLegacyExtensions
    {
        /// <summary>
        /// Gets the total consignor amount for this transaction (sum of all transaction items for the specified consignor)
        /// </summary>
        public static decimal ConsignorAmount(this Transaction transaction, Guid? consignorId = null)
        {
            if (consignorId.HasValue)
            {
                return transaction.Items.Where(ti => ti.ConsignorId == consignorId.Value).Sum(ti => ti.ConsignorAmount);
            }
            return transaction.Items.Sum(ti => ti.ConsignorAmount);
        }

        /// <summary>
        /// Gets the total shop amount for this transaction (sum of all transaction items)
        /// </summary>
        public static decimal ShopAmount(this Transaction transaction)
        {
            return transaction.Items.Sum(ti => ti.StoreAmount);
        }

        /// <summary>
        /// Gets the sale price (total amount) for this transaction
        /// </summary>
        public static decimal SalePrice(this Transaction transaction)
        {
            return transaction.Total;
        }

        /// <summary>
        /// Gets the payment method (maps to PaymentType)
        /// </summary>
        public static string PaymentMethod(this Transaction transaction)
        {
            return transaction.PaymentType;
        }

        /// <summary>
        /// Gets the sale date (maps to TransactionDate)
        /// </summary>
        public static DateTime SaleDate(this Transaction transaction)
        {
            return transaction.TransactionDate;
        }

        /// <summary>
        /// Gets the first item in the transaction (for legacy single-item transactions)
        /// </summary>
        public static Item? Item(this Transaction transaction)
        {
            return transaction.Items.FirstOrDefault()?.Item;
        }

        /// <summary>
        /// Gets the first consignor in the transaction (for legacy single-item transactions)
        /// </summary>
        public static Consignor? Consignor(this Transaction transaction)
        {
            return transaction.Items.FirstOrDefault()?.Consignor;
        }

        /// <summary>
        /// Gets the first consignor ID in the transaction (for legacy compatibility)
        /// </summary>
        public static Guid? ConsignorId(this Transaction transaction)
        {
            return transaction.Items.FirstOrDefault()?.ConsignorId;
        }

        /// <summary>
        /// Gets the first item ID in the transaction (for legacy compatibility)
        /// </summary>
        public static Guid? ItemId(this Transaction transaction)
        {
            return transaction.Items.FirstOrDefault()?.ItemId;
        }

        /// <summary>
        /// Gets the first item name in the transaction (for legacy compatibility)
        /// </summary>
        public static string? ItemName(this Transaction transaction)
        {
            return transaction.Items.FirstOrDefault()?.Item?.Title;
        }
    }
}