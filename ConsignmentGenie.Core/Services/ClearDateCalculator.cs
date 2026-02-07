using ConsignmentGenie.Core.Entities;

namespace ConsignmentGenie.Core.Services;

public interface IClearDateCalculator
{
    DateTime CalculateClearDate(DateTime saleDate, string paymentMethod, PayoutSettings settings);
    bool IsCleared(DateTime clearDate);
}

public class ClearDateCalculator : IClearDateCalculator
{
    public DateTime CalculateClearDate(DateTime saleDate, string paymentMethod, PayoutSettings settings)
    {
        var clearanceDays = GetClearanceDays(paymentMethod, settings);
        return saleDate.Date.AddDays(clearanceDays);
    }

    public bool IsCleared(DateTime clearDate)
    {
        return DateTime.UtcNow.Date >= clearDate.Date;
    }

    private int GetClearanceDays(string paymentMethod, PayoutSettings settings)
    {
        // Cash sales are immediately payout-eligible (money is already in hand)
        if (paymentMethod.Equals("Cash", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        // All other payment methods (Card, Check, Other) use the configured hold period
        return settings.HoldPeriodDays;
    }
}