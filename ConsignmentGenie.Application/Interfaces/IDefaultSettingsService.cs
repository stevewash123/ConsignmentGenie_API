namespace ConsignmentGenie.Application.Interfaces;

/// <summary>
/// Centralized service for creating default settings for new users
/// Provides a single location for all default setting creation logic
/// </summary>
public interface IDefaultSettingsService
{
    /// <summary>
    /// Creates all default settings for a new owner
    /// Includes notification preferences, organization settings, business settings, etc.
    /// </summary>
    Task CreateOwnerDefaultsAsync(Guid userId, Guid organizationId, string email);

    /// <summary>
    /// Creates all default settings for a new consignor
    /// Includes notification preferences, profile settings, communication preferences, etc.
    /// </summary>
    Task CreateConsignorDefaultsAsync(Guid userId, string email);

    /// <summary>
    /// Creates all default settings for a new customer
    /// Includes notification preferences for reservations, expiration alerts, etc.
    /// </summary>
    Task CreateCustomerDefaultsAsync(Guid userId, string email);
}