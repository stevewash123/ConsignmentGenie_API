using ConsignmentGenie.Core.Entities;

namespace ConsignmentGenie.Core.Extensions;

public static class TransactionExtensions
{
    /// <summary>
    /// Gets the total sale price for backward compatibility
    /// </summary>
    public static decimal GetSalePrice(this Transaction transaction)
    {
        return transaction.Total;
    }

    /// <summary>
    /// Gets total consignor amount across all items
    /// </summary>
    public static decimal GetConsignorAmount(this Transaction transaction)
    {
        return transaction.Items?.Sum(ti => ti.ConsignorAmount) ?? 0;
    }

    /// <summary>
    /// Gets total shop amount across all items
    /// </summary>
    public static decimal GetShopAmount(this Transaction transaction)
    {
        return transaction.Items?.Sum(ti => ti.StoreAmount) ?? 0;
    }

    /// <summary>
    /// Gets the first item for backward compatibility
    /// </summary>
    public static Item? GetFirstItem(this Transaction transaction)
    {
        return transaction.Items?.FirstOrDefault()?.Item;
    }

    /// <summary>
    /// Gets the first consignor for backward compatibility
    /// </summary>
    public static Consignor? GetFirstConsignor(this Transaction transaction)
    {
        return transaction.Items?.FirstOrDefault()?.Consignor;
    }

    /// <summary>
    /// Gets the first consignor ID for backward compatibility
    /// </summary>
    public static Guid? GetFirstConsignorId(this Transaction transaction)
    {
        return transaction.Items?.FirstOrDefault()?.ConsignorId;
    }

    /// <summary>
    /// Gets the first item ID for backward compatibility
    /// </summary>
    public static Guid? GetFirstItemId(this Transaction transaction)
    {
        return transaction.Items?.FirstOrDefault()?.ItemId;
    }

    /// <summary>
    /// Gets payment method with proper field name mapping
    /// </summary>
    public static string? GetPaymentMethod(this Transaction transaction)
    {
        return transaction.PaymentType;
    }
}