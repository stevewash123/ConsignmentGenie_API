using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Interfaces;
using ConsignmentGenie.Core.DTOs;
using ConsignmentGenie.Core.DTOs.Items;
using ConsignmentGenie.Core.Enums;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text.Json;

namespace ConsignmentGenie.Application.Services;

public class SquareInventoryService : ISquareInventoryService
{
    private readonly HttpClient _httpClient;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SquareInventoryService> _logger;

    public SquareInventoryService(HttpClient httpClient, IUnitOfWork unitOfWork, ILogger<SquareInventoryService> logger)
    {
        _httpClient = httpClient;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<PagedResult<ItemListDto>> GetSquareItemsAsync(string organizationId, int page = 1, int pageSize = 25, string? search = null)
    {
        try
        {
            var connection = await GetSquareConnectionAsync(organizationId);
            if (connection == null)
            {
                return new PagedResult<ItemListDto>
                {
                    Data = new List<ItemListDto>(),
                    TotalCount = 0,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = 0
                };
            }

            // Set up Square API headers
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", connection.AccessToken);
            _httpClient.DefaultRequestHeaders.Add("Square-Version", "2023-10-18");

            // Build Square Catalog API request
            var url = "https://connect.squareup.com/v2/catalog/list?types=ITEM";

            if (!string.IsNullOrEmpty(search))
            {
                // Square doesn't support direct text search in catalog/list, we'll filter after retrieval
                // For production, consider using the Search endpoint: /v2/catalog/search
            }

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch Square catalog: {StatusCode} {Content}", response.StatusCode, await response.Content.ReadAsStringAsync());
                throw new Exception($"Square API error: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var catalogResponse = JsonSerializer.Deserialize<SquareCatalogResponse>(content, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (catalogResponse?.Objects == null)
            {
                return new PagedResult<ItemListDto>
                {
                    Data = new List<ItemListDto>(),
                    TotalCount = 0,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = 0
                };
            }

            // Convert Square items to our DTO format
            var squareItems = catalogResponse.Objects
                .Where(obj => obj.Type == "ITEM" && obj.ItemData != null)
                .Select(ConvertToItemListDto)
                .Where(item => item != null)
                .Cast<ItemListDto>()
                .ToList();

            // Apply search filter if provided
            if (!string.IsNullOrEmpty(search))
            {
                squareItems = squareItems.Where(item =>
                    item.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    item.Sku.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (item.Category?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
                ).ToList();
            }

            // Apply pagination
            var totalCount = squareItems.Count;
            var pagedItems = squareItems
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedResult<ItemListDto>
            {
                Data = pagedItems,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Square inventory for organization {OrganizationId}", organizationId);
            throw;
        }
    }

    public async Task<ItemDetailDto?> GetSquareItemAsync(string organizationId, string itemId)
    {
        try
        {
            var connection = await GetSquareConnectionAsync(organizationId);
            if (connection == null)
                return null;

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", connection.AccessToken);
            _httpClient.DefaultRequestHeaders.Add("Square-Version", "2023-10-18");

            var response = await _httpClient.GetAsync($"https://connect.squareup.com/v2/catalog/object/{itemId}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch Square item {ItemId}: {StatusCode}", itemId, response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var itemResponse = JsonSerializer.Deserialize<SquareCatalogObjectResponse>(content, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (itemResponse?.Object?.Type == "ITEM" && itemResponse.Object.ItemData != null)
            {
                return ConvertToItemDetailDto(itemResponse.Object);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Square item {ItemId} for organization {OrganizationId}", itemId, organizationId);
            throw;
        }
    }

    public async Task SyncSquareInventoryAsync(string organizationId)
    {
        try
        {
            var connection = await GetSquareConnectionAsync(organizationId);
            if (connection == null)
                throw new InvalidOperationException("Square connection not found");

            // Get organization to update integration mode
            var organization = await _unitOfWork.Organizations.GetByIdAsync(Guid.Parse(organizationId));
            if (organization == null)
                throw new InvalidOperationException("Organization not found");

            _logger.LogInformation("Starting Square inventory sync for organization {OrganizationId}", organizationId);

            // Record sync start in log
            var syncLog = new SquareSyncLog
            {
                OrganizationId = Guid.Parse(organizationId),
                SquareConnectionId = connection.Id,
                SyncStarted = DateTime.UtcNow,
                Success = false
            };

            await _unitOfWork.SquareSyncLogs.AddAsync(syncLog);

            try
            {
                // Step 1: Get all CG items for this organization
                var cgItems = await _unitOfWork.Items.GetAllAsync(i => i.OrganizationId == Guid.Parse(organizationId));

                // Step 2: Push CG items to Square (if they don't exist there)
                await PushCgItemsToSquareAsync(connection, cgItems);

                // Step 3: Get all Square items and create proxy entries in CG
                await PullSquareItemsToCgAsync(organizationId, connection);

                // Step 4: Set organization to use Square as inventory source
                organization.IntegrationMode = IntegrationMode.Square;

                // Update connection last sync time
                connection.LastSyncAt = DateTime.UtcNow;

                // Complete sync log
                syncLog.SyncCompleted = DateTime.UtcNow;
                syncLog.Success = true;
                syncLog.Details = "Bi-directional inventory sync completed successfully";

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Square inventory sync completed for organization {OrganizationId}", organizationId);
            }
            catch (Exception)
            {
                syncLog.Success = false;
                syncLog.Details = "Sync failed during data transfer";
                await _unitOfWork.SaveChangesAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing Square inventory for organization {OrganizationId}", organizationId);
            throw;
        }
    }

    public async Task SyncToCgNativeAsync(string organizationId)
    {
        try
        {
            var organization = await _unitOfWork.Organizations.GetByIdAsync(Guid.Parse(organizationId));
            if (organization == null)
                throw new InvalidOperationException("Organization not found");

            _logger.LogInformation("Starting CG Native inventory sync for organization {OrganizationId}", organizationId);

            // Get Square connection if available
            var connection = await GetSquareConnectionAsync(organizationId);

            if (connection != null)
            {
                try
                {
                    // Step 1: Get all Square items and sync them to CG as regular items
                    await PullSquareItemsAsCgNativeAsync(organizationId, connection);

                    // Step 2: Ensure Square has copies of existing CG items for payment processing
                    var cgItems = await _unitOfWork.Items.GetAllAsync(i => i.OrganizationId == Guid.Parse(organizationId));
                    await PushCgItemsToSquareAsync(connection, cgItems.Where(i => !i.IsSquareManaged).ToList());
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Warning during Square->CG sync, continuing with mode switch");
                }
            }

            // Set organization to use CG Native as inventory source
            organization.IntegrationMode = IntegrationMode.CgNative;

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Successfully switched to CG Native inventory for organization {OrganizationId}", organizationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error switching to CG Native inventory for organization {OrganizationId}", organizationId);
            throw;
        }
    }

    private async Task<SquareConnection?> GetSquareConnectionAsync(string organizationId)
    {
        return await _unitOfWork.SquareConnections.GetAsync(sc => sc.OrganizationId == Guid.Parse(organizationId));
    }

    private ItemListDto? ConvertToItemListDto(SquareCatalogObject catalogObject)
    {
        if (catalogObject.ItemData == null) return null;

        var variation = catalogObject.ItemData.Variations?.FirstOrDefault();
        var price = 0m;
        var sku = "SQUARE-" + catalogObject.Id[..8]; // Use first 8 chars of Square ID as fallback SKU

        // Get price from first variation
        if (variation?.ItemVariationData?.PriceMoney != null)
        {
            // Square stores prices in cents
            price = variation.ItemVariationData.PriceMoney.Amount / 100m;
        }

        // Get SKU from first variation
        if (!string.IsNullOrEmpty(variation?.ItemVariationData?.Sku))
        {
            sku = variation.ItemVariationData.Sku;
        }

        return new ItemListDto
        {
            ItemId = GenerateGuidFromSquareId(catalogObject.Id),
            Sku = sku,
            Title = catalogObject.ItemData.Name ?? "Unknown Square Item",
            Price = price,
            Category = catalogObject.ItemData.CategoryId,
            Condition = ItemCondition.New, // Square doesn't have condition, default to new
            Status = ItemStatus.Available, // Default status for Square items
            ReceivedDate = DateTime.TryParse(catalogObject.CreatedAt, out var created) ? created : DateTime.UtcNow,
            SoldDate = null, // Square items are not tracked as sold in our system
            ConsignorId = Guid.Empty, // Square items don't have consignors by default
            ConsignorName = "Square Inventory", // Indicate this is from Square
            CommissionRate = 0m, // No commission for Square items by default
            ConditionLabel = "New"
        };
    }

    private ItemDetailDto? ConvertToItemDetailDto(SquareCatalogObject catalogObject)
    {
        var listDto = ConvertToItemListDto(catalogObject);
        if (listDto == null) return null;

        var now = DateTime.UtcNow;

        return new ItemDetailDto
        {
            ItemId = listDto.ItemId,
            ConsignorId = listDto.ConsignorId,
            ConsignorName = listDto.ConsignorName,
            CommissionRate = listDto.CommissionRate,
            Sku = listDto.Sku,
            Title = listDto.Title,
            Description = catalogObject.ItemData?.Description,
            Category = listDto.Category,
            Condition = listDto.Condition,
            ConditionLabel = listDto.ConditionLabel,
            Price = listDto.Price,
            ShopAmount = listDto.Price, // For Square items, no commission split by default
            ConsignorAmount = 0m, // No commission for Square items by default
            Status = listDto.Status,
            ReceivedDate = listDto.ReceivedDate,
            SoldDate = listDto.SoldDate,
            Images = new List<ItemImageDto>(), // TODO: Handle Square item images
            Photos = new List<PhotoInfoDto>(), // TODO: Handle Square item images
            Notes = "Imported from Square",
            CreatedAt = DateTime.TryParse(catalogObject.CreatedAt, out var created) ? created : now,
            UpdatedAt = DateTime.TryParse(catalogObject.UpdatedAt, out var updated) ? updated : now
        };
    }

    private Guid GenerateGuidFromSquareId(string squareId)
    {
        // Create a deterministic GUID from Square ID using MD5 hash
        using var md5 = System.Security.Cryptography.MD5.Create();
        var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes("SQUARE:" + squareId));
        return new Guid(hash);
    }

    public async Task SyncAllAsync(string organizationId)
    {
        _logger.LogInformation("Starting complete Square sync (all items and categories) for organization {OrganizationId}", organizationId);

        // First sync categories
        await SyncCategoriesAsync(organizationId);

        // Then sync all inventory items
        await SyncToCgNativeAsync(organizationId);
    }

    public async Task SyncItemAsync(string organizationId, string itemId)
    {
        _logger.LogInformation("Starting single item sync for Square item {ItemId} in organization {OrganizationId}", itemId, organizationId);

        var connection = await GetSquareConnectionAsync(organizationId);
        if (connection == null)
        {
            throw new InvalidOperationException("Square connection not found");
        }

        // Since items can reference categories, sync categories first to ensure data consistency
        await SyncCategoriesAsync(organizationId);

        // Get the specific item from Square
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", connection.AccessToken);
        _httpClient.DefaultRequestHeaders.Add("Square-Version", "2023-10-18");

        var url = $"https://connect.squareup.com/v2/catalog/object/{itemId}";
        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to fetch Square item {itemId}: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var catalogResponse = JsonSerializer.Deserialize<SquareCatalogObjectResponse>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var squareItem = catalogResponse?.Object;
        if (squareItem == null)
        {
            throw new Exception($"Square item {itemId} not found");
        }

        // Sync this specific item
        await CreateCgProxyItemAsync(organizationId, squareItem);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Successfully synced Square item {ItemId} to CG", itemId);
    }

    public async Task SyncCategoriesAsync(string organizationId)
    {
        _logger.LogInformation("Starting Square categories sync for organization {OrganizationId}", organizationId);

        var connection = await GetSquareConnectionAsync(organizationId);
        if (connection == null)
        {
            throw new InvalidOperationException("Square connection not found");
        }

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", connection.AccessToken);
        _httpClient.DefaultRequestHeaders.Add("Square-Version", "2023-10-18");

        // Get all categories from Square
        var url = "https://connect.squareup.com/v2/catalog/list?types=CATEGORY";
        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to fetch Square categories: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var catalogResponse = JsonSerializer.Deserialize<SquareCatalogResponse>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var categories = catalogResponse?.Objects?.Where(obj => obj.Type == "CATEGORY").ToList() ?? new List<SquareCatalogObject>();

        foreach (var category in categories)
        {
            try
            {
                // Skip if no category data or name
                if (category.CategoryData == null || string.IsNullOrWhiteSpace(category.CategoryData.Name))
                {
                    continue;
                }

                var categoryName = category.CategoryData.Name;

                // Check if category already exists in CG
                var existingCategory = await _unitOfWork.ItemCategories.GetAsync(c =>
                    c.OrganizationId == Guid.Parse(organizationId) &&
                    c.Name == categoryName);

                if (existingCategory == null)
                {
                    // Create new category in CG
                    var newCategory = new ItemCategory
                    {
                        Id = Guid.NewGuid(),
                        OrganizationId = Guid.Parse(organizationId),
                        Name = categoryName,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _unitOfWork.ItemCategories.AddAsync(newCategory);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to sync Square category {CategoryId}: {CategoryName}",
                    category.Id, category.CategoryData?.Name);
            }
        }

        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Successfully synced {Count} categories from Square", categories.Count);
    }

    private async Task PushCgItemsToSquareAsync(SquareConnection connection, IEnumerable<Item> cgItems)
    {
        _logger.LogInformation("Pushing {Count} CG items to Square", cgItems.Count());

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", connection.AccessToken);
        _httpClient.DefaultRequestHeaders.Add("Square-Version", "2023-10-18");

        foreach (var item in cgItems.Where(i => string.IsNullOrEmpty(i.SquareCatalogId)))
        {
            try
            {
                await CreateSquareItemAsync(item);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to sync CG item {ItemId} to Square", item.Id);
            }
        }
    }

    private async Task PullSquareItemsToCgAsync(string organizationId, SquareConnection connection)
    {
        _logger.LogInformation("Pulling Square items to CG as proxies for organization {OrganizationId}", organizationId);

        // Get Square items
        var squareItems = await GetAllSquareItemsAsync(connection);

        foreach (var squareItem in squareItems)
        {
            try
            {
                // Check if we already have a proxy for this Square item
                var existingProxy = await _unitOfWork.Items.GetAsync(i => i.SquareCatalogId == squareItem.Id);
                if (existingProxy == null)
                {
                    await CreateCgProxyItemAsync(organizationId, squareItem);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create CG proxy for Square item {SquareItemId}", squareItem.Id);
            }
        }
    }

    private async Task PullSquareItemsAsCgNativeAsync(string organizationId, SquareConnection connection)
    {
        _logger.LogInformation("Converting Square items to CG native items for organization {OrganizationId}", organizationId);

        // Get Square items
        var squareItems = await GetAllSquareItemsAsync(connection);

        foreach (var squareItem in squareItems)
        {
            try
            {
                // Check if we have a proxy that can be converted
                var existingProxy = await _unitOfWork.Items.GetAsync(i => i.SquareCatalogId == squareItem.Id);
                if (existingProxy?.IsSquareManaged == true)
                {
                    // Convert proxy to native CG item
                    existingProxy.IsSquareManaged = false;
                    existingProxy.SquareCatalogId = null;
                    existingProxy.SquareVariationId = null;
                    existingProxy.SquareLastSyncAt = null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to convert Square proxy item {SquareItemId} to native", squareItem.Id);
            }
        }
    }

    private async Task<List<SquareCatalogObject>> GetAllSquareItemsAsync(SquareConnection connection)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", connection.AccessToken);
        _httpClient.DefaultRequestHeaders.Add("Square-Version", "2023-10-18");

        var url = "https://connect.squareup.com/v2/catalog/list?types=ITEM";
        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Square API error: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var catalogResponse = JsonSerializer.Deserialize<SquareCatalogResponse>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return catalogResponse?.Objects?.Where(obj => obj.Type == "ITEM" && obj.ItemData != null).ToList() ?? new List<SquareCatalogObject>();
    }

    private async Task CreateSquareItemAsync(Item cgItem)
    {
        var squareItemData = new
        {
            type = "ITEM",
            id = $"#{Guid.NewGuid()}",
            item_data = new
            {
                name = cgItem.Title,
                description = cgItem.Description,
                variations = new[]
                {
                    new
                    {
                        type = "ITEM_VARIATION",
                        id = $"#{Guid.NewGuid()}",
                        item_variation_data = new
                        {
                            name = cgItem.Title,
                            sku = cgItem.Sku,
                            pricing_type = "FIXED_PRICING",
                            price_money = new
                            {
                                amount = (long)(cgItem.Price * 100), // Convert to cents
                                currency = "USD"
                            }
                        }
                    }
                }
            }
        };

        var requestBody = new { @object = squareItemData };
        var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("https://connect.squareup.com/v2/catalog/object", content);

        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<SquareCatalogObjectResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (result?.Object != null)
            {
                cgItem.SquareCatalogId = result.Object.Id;
                cgItem.SquareVariationId = result.Object.ItemData?.Variations?.FirstOrDefault()?.Id;
                cgItem.SquareLastSyncAt = DateTime.UtcNow;
                await _unitOfWork.SaveChangesAsync();
            }
        }
    }

    private async Task CreateCgProxyItemAsync(string organizationId, SquareCatalogObject squareItem)
    {
        var variation = squareItem.ItemData?.Variations?.FirstOrDefault();
        var price = 0m;

        if (variation?.ItemVariationData?.PriceMoney != null)
        {
            price = variation.ItemVariationData.PriceMoney.Amount / 100m;
        }

        var proxyItem = new Item
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.Parse(organizationId),
            ConsignorId = Guid.Empty, // Will need a default "Square" consignor
            Sku = variation?.ItemVariationData?.Sku ?? $"SQUARE-{squareItem.Id[..8]}",
            Title = squareItem.ItemData?.Name ?? "Unknown Square Item",
            Description = squareItem.ItemData?.Description,
            Price = price,
            Condition = ItemCondition.New,
            Status = ItemStatus.Available,
            ReceivedDate = DateOnly.FromDateTime(DateTime.UtcNow),
            SquareCatalogId = squareItem.Id,
            SquareVariationId = variation?.Id,
            IsSquareManaged = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Items.AddAsync(proxyItem);
    }
}

// Square API response models
public class SquareCatalogResponse
{
    public List<SquareCatalogObject>? Objects { get; set; }
    public string? Cursor { get; set; }
}

public class SquareCatalogObjectResponse
{
    public SquareCatalogObject? Object { get; set; }
}

public class SquareCatalogObject
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? CreatedAt { get; set; }
    public string? UpdatedAt { get; set; }
    public bool? IsDeleted { get; set; }
    public SquareItemData? ItemData { get; set; }
    public SquareCategoryData? CategoryData { get; set; }
}

public class SquareItemData
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? CategoryId { get; set; }
    public List<SquareItemVariation>? Variations { get; set; }
}

public class SquareCategoryData
{
    public string? Name { get; set; }
}

public class SquareItemVariation
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public SquareItemVariationData? ItemVariationData { get; set; }
}

public class SquareItemVariationData
{
    public string? Name { get; set; }
    public string? Sku { get; set; }
    public SquarePriceMoney? PriceMoney { get; set; }
}

public class SquarePriceMoney
{
    public long Amount { get; set; }
    public string Currency { get; set; } = "USD";
}