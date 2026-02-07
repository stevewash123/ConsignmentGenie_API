using ConsignmentGenie.Application.DTOs.Storefront;

namespace ConsignmentGenie.Application.Services.Interfaces;

public interface IStoreService
{
    Task<StoreInfoDto?> GetStoreInfoAsync(string storeSlug);
    Task<(List<PublicItemDto> items, int totalCount)> GetItemsAsync(string storeSlug, ItemQueryParams queryParams);
    Task<PublicItemDetailDto?> GetItemDetailAsync(string storeSlug, Guid itemId);
    Task<List<CategoryDto>> GetCategoriesAsync(string storeSlug);
}