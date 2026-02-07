namespace ConsignmentGenie.Core.Enums;

/// <summary>
/// Smart enum that defines the different settings pages/sections in the application
/// Each page corresponds to a JSON column in the Organization table
/// </summary>
public sealed class SettingsPage : IEquatable<SettingsPage>
{
    private static readonly Dictionary<string, SettingsPage> _pages = new();

    /// <summary>
    /// Organization-level settings (basic info, branding, contact)
    /// Maps to Organization.OrganizationSettings JSON column
    /// </summary>
    public static readonly SettingsPage Organization = new("Organization", "OrganizationSettings");

    /// <summary>
    /// Business settings (tax, policies, store hours, payments)
    /// Maps to Organization.BusinessSettings JSON column
    /// </summary>
    public static readonly SettingsPage Business = new("Business", "BusinessSettings");

    /// <summary>
    /// Consignor management settings (onboarding, agreements, approval)
    /// Maps to Organization.ConsignorSettings JSON column
    /// </summary>
    public static readonly SettingsPage Consignor = new("Consignor", "ConsignorSettings");

    /// <summary>
    /// Storefront/customer-facing settings (appearance, checkout)
    /// Maps to Organization.StorefrontSettings JSON column
    /// </summary>
    public static readonly SettingsPage Storefront = new("Storefront", "StorefrontSettings");

    /// <summary>
    /// Notification and email settings
    /// Maps to Organization.NotificationSettings JSON column
    /// </summary>
    public static readonly SettingsPage Notifications = new("Notifications", "NotificationSettings");

    /// <summary>
    /// Receipt formatting and printing settings
    /// Maps to Organization.ReceiptSettings JSON column
    /// </summary>
    public static readonly SettingsPage Receipts = new("Receipts", "ReceiptSettings");

    public string Name { get; }
    public string JsonColumnName { get; }

    private SettingsPage(string name, string jsonColumnName)
    {
        Name = name;
        JsonColumnName = jsonColumnName;
        _pages[name] = this;
    }

    /// <summary>
    /// Get all available settings pages
    /// </summary>
    public static IEnumerable<SettingsPage> All => _pages.Values;

    /// <summary>
    /// Get a settings page by name
    /// </summary>
    public static SettingsPage? FromName(string name) => _pages.TryGetValue(name, out var page) ? page : null;

    public bool Equals(SettingsPage? other) => other is not null && Name == other.Name;

    public override bool Equals(object? obj) => Equals(obj as SettingsPage);

    public override int GetHashCode() => Name.GetHashCode();

    public override string ToString() => Name;

    public static bool operator ==(SettingsPage? left, SettingsPage? right) =>
        ReferenceEquals(left, right) || (left?.Equals(right) == true);

    public static bool operator !=(SettingsPage? left, SettingsPage? right) => !(left == right);
}