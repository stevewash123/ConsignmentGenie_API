namespace ConsignmentGenie.Core.Constants
{
    /// <summary>
    /// Common tax exemption reasons
    /// </summary>
    public static class TaxExemptReasons
    {
        public const string Nonprofit = "Nonprofit organization";
        public const string Reseller = "Reseller (resale certificate)";
        public const string ExemptCategory = "Exempt product category";
        public const string Other = "Other";

        /// <summary>
        /// Gets all valid tax exemption reasons
        /// </summary>
        public static readonly string[] All = {
            Nonprofit,
            Reseller,
            ExemptCategory,
            Other
        };

        /// <summary>
        /// Validates if the provided reason is valid
        /// </summary>
        /// <param name="reason">Reason to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool IsValidReason(string? reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                return false;

            return All.Contains(reason, StringComparer.OrdinalIgnoreCase);
        }
    }
}