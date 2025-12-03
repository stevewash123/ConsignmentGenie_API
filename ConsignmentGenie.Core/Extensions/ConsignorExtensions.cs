namespace ConsignmentGenie.Core.Extensions;

using ConsignmentGenie.Core.Entities;

public static class ProviderExtensions
{
    public static string GetDisplayName(this Consignor provider)
    {
        return $"{provider.FirstName} {provider.LastName}".Trim();
    }

    public static string GetFullAddress(this Consignor provider)
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(provider.AddressLine1))
            parts.Add(provider.AddressLine1);

        if (!string.IsNullOrEmpty(provider.AddressLine2))
            parts.Add(provider.AddressLine2);

        if (!string.IsNullOrEmpty(provider.City))
            parts.Add(provider.City);

        if (!string.IsNullOrEmpty(provider.State))
            parts.Add(provider.State);

        if (!string.IsNullOrEmpty(provider.PostalCode))
            parts.Add(provider.PostalCode);

        return string.Join(", ", parts);
    }
}