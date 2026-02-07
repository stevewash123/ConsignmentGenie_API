using ConsignmentGenie.Application.DTOs.Shopper;

namespace ConsignmentGenie.Application.Services.Interfaces;

public interface IShopperAuthService
{
    /// <summary>
    /// Registers a new shopper for a specific store
    /// </summary>
    /// <param name="request">Registration details</param>
    /// <param name="storeSlug">Store slug identifier</param>
    /// <returns>Authentication result with token and profile</returns>
    Task<AuthResultDto> RegisterAsync(ShopperRegisterRequest request, string storeSlug);

    /// <summary>
    /// Authenticates a shopper for a specific store
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <param name="storeSlug">Store slug identifier</param>
    /// <returns>Authentication result with token and profile</returns>
    Task<AuthResultDto> LoginAsync(ShopperLoginRequest request, string storeSlug);

    /// <summary>
    /// Creates a guest checkout session
    /// </summary>
    /// <param name="request">Guest session details</param>
    /// <param name="storeSlug">Store slug identifier</param>
    /// <returns>Guest session token and expiry</returns>
    Task<GuestSessionDto> CreateGuestSessionAsync(GuestSessionRequest request, string storeSlug);

    /// <summary>
    /// Gets shopper profile by user ID and organization
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="organizationId">Organization ID</param>
    /// <returns>Shopper profile</returns>
    Task<ShopperProfileDto?> GetShopperProfileAsync(Guid userId, Guid organizationId);

    /// <summary>
    /// Updates shopper profile
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="request">Profile update request</param>
    /// <returns>Updated profile</returns>
    Task<ShopperProfileDto?> UpdateShopperProfileAsync(Guid userId, Guid organizationId, UpdateShopperProfileRequest request);

    /// <summary>
    /// Changes shopper password
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="request">Password change request</param>
    /// <returns>Success indicator</returns>
    Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request);

    /// <summary>
    /// Generates JWT token for shopper with store-specific claims
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="shopperId">Shopper ID</param>
    /// <param name="email">Email</param>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="storeSlug">Store slug</param>
    /// <returns>JWT token</returns>
    string GenerateShopperJwtToken(Guid userId, Guid shopperId, string email, Guid organizationId, string storeSlug);
}