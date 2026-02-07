namespace ConsignmentGenie.Core.Extensions;

using ConsignmentGenie.Core.Entities;

public static class ConsignorExtensions
{
    public static string GetDisplayName(this Consignor consignor)
    {
        return $"{consignor.FirstName} {consignor.LastName}".Trim();
    }

    public static string GetFullAddress(this Consignor consignor)
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(consignor.AddressLine1))
            parts.Add(consignor.AddressLine1);

        if (!string.IsNullOrEmpty(consignor.AddressLine2))
            parts.Add(consignor.AddressLine2);

        if (!string.IsNullOrEmpty(consignor.City))
            parts.Add(consignor.City);

        if (!string.IsNullOrEmpty(consignor.State))
            parts.Add(consignor.State);

        if (!string.IsNullOrEmpty(consignor.PostalCode))
            parts.Add(consignor.PostalCode);

        return string.Join(", ", parts);
    }
}