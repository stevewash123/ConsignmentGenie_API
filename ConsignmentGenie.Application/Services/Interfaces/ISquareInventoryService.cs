using ConsignmentGenie.Core.DTOs;
using ConsignmentGenie.Core.DTOs.Items;

namespace ConsignmentGenie.Application.Services.Interfaces;

public interface ISquareInventoryService
{
    Task<PagedResult<ItemListDto>> GetSquareItemsAsync(string organizationId, int page = 1, int pageSize = 25, string? search = null);
    Task<ItemDetailDto?> GetSquareItemAsync(string organizationId, string itemId);
    Task SyncSquareInventoryAsync(string organizationId);
    Task SyncToCgNativeAsync(string organizationId);

    // Granular sync methods
    Task SyncAllAsync(string organizationId);
    Task SyncItemAsync(string organizationId, string itemId);
    Task SyncCategoriesAsync(string organizationId);
}

public class SquareItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Sku { get; set; }
    public decimal? Price { get; set; }
    public string? Category { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<string> ImageUrls { get; set; } = new();

    // Consignment-specific fields (tracked locally)
    public string? ConsignorId { get; set; }
    public string? ConsignorName { get; set; }
    public decimal? ConsignmentRate { get; set; }
    public string? ConsignmentStatus { get; set; }
    public DateTime? ConsignmentStartDate { get; set; }
    public DateTime? ConsignmentEndDate { get; set; }
}