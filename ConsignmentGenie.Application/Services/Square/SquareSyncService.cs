using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Core.DTOs.Square;
using ConsignmentGenie.Core.DTOs.Notifications;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ConsignmentGenie.Application.Services.Square;

public class SquareSyncService : ISquareSyncService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SquareSyncService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IPendingImportItemService _pendingImportItemService;
    private readonly INotificationService _notificationService;
    private readonly string _squareBaseUrl;

    public SquareSyncService(
        IUnitOfWork unitOfWork,
        ILogger<SquareSyncService> logger,
        HttpClient httpClient,
        IConfiguration configuration,
        IPendingImportItemService pendingImportItemService,
        INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _httpClient = httpClient;
        _configuration = configuration;
        _pendingImportItemService = pendingImportItemService;
        _notificationService = notificationService;
        _squareBaseUrl = _configuration["Square:BaseUrl"] ?? "https://connect.squareupsandbox.com";
    }

    public async Task<CategorySyncResult> SyncCategoriesAsync(Guid shopId)
    {
        _logger.LogInformation("Starting Square categories sync for organization {ShopId}", shopId);

        var connection = await GetSquareConnectionAsync(shopId);
        if (connection == null)
        {
            throw new InvalidOperationException("Square connection not found");
        }

        SetSquareApiHeaders(connection.AccessToken);

        // Get all categories from Square
        var url = $"{_squareBaseUrl}/v2/catalog/list?types=CATEGORY";
        _logger.LogInformation("🏷️ [SyncCategoriesAsync] Calling Square Categories API: {Url}", url);
        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to fetch Square categories: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var catalogResponse = JsonSerializer.Deserialize<SquareCatalogResponse>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });

        var categories = catalogResponse?.Objects?.Where(obj => obj.Type == "CATEGORY").ToList() ?? new List<SquareCatalogObject>();
        int created = 0, updated = 0;

        _logger.LogInformation("🏷️ [SyncCategoriesAsync] Found {CategoryCount} categories from Square API", categories.Count);
        if (categories.Count > 0)
        {
            _logger.LogInformation("🏷️ [SyncCategoriesAsync] First category example: ID={Id}, Name={Name}",
                categories.First().Id, categories.First().CategoryData?.Name ?? "NULL");
        }

        foreach (var category in categories)
        {
            try
            {
                // Skip if no category data or name
                if (category.CategoryData == null || string.IsNullOrWhiteSpace(category.CategoryData.Name))
                {
                    _logger.LogDebug("🏷️ Skipping category {CategoryId} - no name", category.Id);
                    continue;
                }

                var categoryName = category.CategoryData.Name;
                var squareCategoryId = category.Id;

                _logger.LogInformation("🏷️ Processing category: {CategoryName} (Square ID: {SquareId})", categoryName, squareCategoryId);

                // Amendment 1: Look for existing Square category by Square ID only
                // We don't automatically merge with CG categories by name anymore - that would trigger overlap detection
                var existingSquareCategory = await _unitOfWork.ItemCategories.GetAsync(c =>
                    c.OrganizationId == shopId &&
                    c.SquareCategoryId == squareCategoryId &&
                    c.Source == CategorySource.Square);

                if (existingSquareCategory == null)
                {
                    // Create new category in CG with Square source (Amendment 1)
                    var newCategory = new ItemCategory
                    {
                        Id = Guid.NewGuid(),
                        OrganizationId = shopId,
                        Name = categoryName,
                        SquareCategoryId = squareCategoryId,
                        Source = CategorySource.Square,  // Amendment 1: Track source
                        Status = CategoryStatus.Active,   // Square categories are always active
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _unitOfWork.ItemCategories.AddAsync(newCategory);

                    // Save immediately to avoid batch issues
                    try
                    {
                        await _unitOfWork.SaveChangesAsync();
                        created++;
                        _logger.LogInformation("🏷️ ✅ Created new category: {CategoryName} (Square ID: {SquareId})", categoryName, squareCategoryId);
                    }
                    catch (Exception saveEx)
                    {
                        _logger.LogWarning(saveEx, "🏷️ ❌ Failed to save category {CategoryName}: {Error}", categoryName, saveEx.Message);
                    }
                }
                else
                {
                    // Update existing Square category if name has changed
                    bool needsUpdate = false;
                    if (existingSquareCategory.Name != categoryName)
                    {
                        existingSquareCategory.Name = categoryName;
                        needsUpdate = true;
                        _logger.LogInformation("🏷️ 📝 Square category renamed: {OldName} → {NewName}", existingSquareCategory.Name, categoryName);
                    }

                    if (needsUpdate)
                    {
                        existingSquareCategory.UpdatedAt = DateTime.UtcNow;
                        await _unitOfWork.ItemCategories.UpdateAsync(existingSquareCategory);
                        await _unitOfWork.SaveChangesAsync();
                        updated++;
                        _logger.LogInformation("🏷️ ✅ Updated existing Square category: {CategoryName} (Square ID: {SquareId})", categoryName, squareCategoryId);
                    }
                    else
                    {
                        _logger.LogInformation("🏷️ ⏭️ Square category {CategoryName} already exists and is up-to-date", categoryName);
                    }
                }

                // TODO Amendment 1: Add fuzzy matching logic here to detect overlaps with CG categories
                // If this new/updated Square category fuzzy-matches an existing CG category,
                // emit a category_overlap notification for the owner to review
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🏷️ ❌ Failed to sync Square category {CategoryId}: {CategoryName}",
                    category.Id, category.CategoryData?.Name);
            }
        }

        _logger.LogInformation("🏷️ [SyncCategoriesAsync] Completed category sync - Created: {Created}, Updated: {Updated}", created, updated);

        return new CategorySyncResult
        {
            Success = true,
            Created = created,
            Updated = updated,
            Total = categories.Count
        };
    }

    public async Task<FullSyncResult> SyncAllAsync(Guid shopId)
    {
        var stopwatch = Stopwatch.StartNew();
        var errors = new List<string>();

        _logger.LogInformation("Starting complete Square sync (all items and categories) for organization {ShopId}", shopId);

        try
        {
            // Step 1: Sync categories first so items can reference them
            _logger.LogInformation("Starting category sync to ensure items can reference categories");
            var catResult = await SyncCategoriesAsync(shopId);

            // Step 2: Get all items from Square (simplified version for now)
            var connection = await GetSquareConnectionAsync(shopId);
            if (connection == null)
            {
                throw new InvalidOperationException("Square connection not found");
            }

            SetSquareApiHeaders(connection.AccessToken);

            var url = $"{_squareBaseUrl}/v2/catalog/list?types=ITEM";
            _logger.LogInformation("Square API call URL (without location filter): {Url}", url);
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Square API error: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Square API raw response (first 500 chars): {Content}", content.Length > 500 ? content.Substring(0, 500) + "..." : content);

            var catalogResponse = JsonSerializer.Deserialize<SquareCatalogResponse>(content, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            var allObjects = catalogResponse?.Objects ?? new List<SquareCatalogObject>();
            var objectTypes = allObjects.GroupBy(o => o.Type).ToDictionary(g => g.Key, g => g.Count());

            _logger.LogInformation("Square API object types: {ObjectTypes}", string.Join(", ", objectTypes.Select(kvp => $"{kvp.Key}:{kvp.Value}")));

            var itemObjects = allObjects.Where(obj => obj.Type == "ITEM").ToList();
            var itemsWithData = itemObjects.Where(obj => obj.ItemData != null).ToList();
            var itemsWithoutData = itemObjects.Where(obj => obj.ItemData == null).ToList();

            _logger.LogInformation("Items with Type=ITEM: {ItemsTotal}, With ItemData: {ItemsWithData}, Without ItemData: {ItemsWithoutData}",
                itemObjects.Count, itemsWithData.Count, itemsWithoutData.Count);

            // Debug: Check if any items have categories
            var itemsWithCategories = itemsWithData.Where(obj => !string.IsNullOrWhiteSpace(obj.ItemData?.CategoryId)).ToList();
            _logger.LogInformation("🏷️ [SyncAllAsync] Items with categories: {ItemsWithCategories} out of {TotalItems}",
                itemsWithCategories.Count, itemsWithData.Count);

            if (itemsWithCategories.Count > 0)
            {
                var firstItemWithCategory = itemsWithCategories.First();
                _logger.LogInformation("🏷️ [SyncAllAsync] First item with category: Name={Name}, CategoryId={CategoryId}",
                    firstItemWithCategory.ItemData?.Name, firstItemWithCategory.ItemData?.CategoryId);
            }

            var items = itemsWithData;

            int created = 0, updated = 0;

            _logger.LogInformation("🔄 [SyncAllAsync] Processing {ItemCount} Square items...", items.Count);

            foreach (var item in items)
            {
                try
                {
                    _logger.LogInformation("🔄 [SyncAllAsync] Processing item {ItemIndex}/{TotalItems} - Square ID: {SquareItemId}",
                        items.IndexOf(item) + 1, items.Count, item.Id);

                    var result = await UpsertItemFromSquare(shopId, item);
                    if (result.Created) created++;
                    else updated++;

                    _logger.LogInformation("✅ [SyncAllAsync] Item processed - Created: {Created}, Updated: {Updated}", created, updated);
                }
                catch (Exception ex)
                {
                    errors.Add($"Item {item.Id}: {ex.Message}");
                    _logger.LogError(ex, "❌ [SyncAllAsync] Failed to sync Square item {ItemId}", item.Id);
                }
            }

            // Step 3: Sync transactions
            _logger.LogInformation("Starting Square transactions sync for organization {ShopId}", shopId);
            var transactionSyncResult = new TransactionSyncResult { Success = true };
            try
            {
                transactionSyncResult = await SyncTransactionsAsync(shopId);
                if (!transactionSyncResult.Success)
                {
                    errors.AddRange(transactionSyncResult.Errors);
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Transaction sync failed: {ex.Message}");
                _logger.LogError(ex, "Error during transaction sync for organization {ShopId}", shopId);
            }

            // Update last sync time for organization
            var org = await _unitOfWork.Organizations.GetByIdAsync(shopId);
            if (org != null)
            {
                org.QuickBooksLastSync = DateTime.UtcNow; // Reusing this field for now
                await _unitOfWork.SaveChangesAsync();
            }

            stopwatch.Stop();

            // TODO: Send notification to owner about sync completion
            // await SendSyncCompletionNotificationAsync(shopId, created, updated, catResult.Total, transactionSyncResult.TransactionsSynced, errors, stopwatch.Elapsed);

            return new FullSyncResult
            {
                Success = errors.Count == 0,
                CategoriesSynced = catResult.Total,
                ItemsSynced = created + updated,
                ItemsCreated = created,
                ItemsUpdated = updated,
                TransactionsSynced = transactionSyncResult.TransactionsSynced,
                Errors = errors,
                Duration = stopwatch.Elapsed,
                SyncedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            errors.Add($"Sync failed: {ex.Message}");
            _logger.LogError(ex, "Error during SyncAllAsync for organization {ShopId}", shopId);

            return new FullSyncResult
            {
                Success = false,
                Errors = errors,
                Duration = stopwatch.Elapsed,
                SyncedAt = DateTime.UtcNow
            };
        }
    }

    public async Task<ItemSyncResult> SyncItemAsync(Guid shopId, string squareItemId)
    {
        _logger.LogInformation("Starting single item sync for Square item {ItemId} in organization {ShopId}", squareItemId, shopId);

        try
        {
            var connection = await GetSquareConnectionAsync(shopId);
            if (connection == null)
            {
                return new ItemSyncResult
                {
                    Success = false,
                    Error = "Square connection not found"
                };
            }

            // Since items can reference categories, sync categories first to ensure data consistency
            await SyncCategoriesAsync(shopId);

            // Get the specific item from Square
            SetSquareApiHeaders(connection.AccessToken);

            var url = $"{_squareBaseUrl}/v2/catalog/object/{squareItemId}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return new ItemSyncResult
                {
                    Success = false,
                    Error = $"Failed to fetch Square item {squareItemId}: {response.StatusCode}"
                };
            }

            var content = await response.Content.ReadAsStringAsync();
            var catalogResponse = JsonSerializer.Deserialize<SquareCatalogObjectResponse>(content, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            var squareItem = catalogResponse?.Object;
            if (squareItem == null)
            {
                return new ItemSyncResult
                {
                    Success = false,
                    Error = $"Square item {squareItemId} not found"
                };
            }

            // Sync this specific item
            var upsertResult = await UpsertItemFromSquare(shopId, squareItem);
            await _unitOfWork.SaveChangesAsync();

            var variation = squareItem.ItemData?.Variations?.FirstOrDefault();

            return new ItemSyncResult
            {
                Success = true,
                Item = new SyncedItemDto
                {
                    SquareCatalogId = squareItem.Id,
                    SquareVariationId = variation?.Id,
                    Name = squareItem.ItemData?.Name ?? "Unknown Item",
                    Sku = variation?.ItemVariationData?.Sku,
                    Price = (variation?.ItemVariationData?.PriceMoney?.Amount ?? 0) / 100m,
                    CategoryName = upsertResult.CategoryName,
                    Quantity = 1, // Simplified for now
                    Available = true // Simplified for now
                },
                CgItemId = upsertResult.CgItemId,
                SyncedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing Square item {ItemId} for organization {ShopId}", squareItemId, shopId);
            return new ItemSyncResult
            {
                Success = false,
                Error = ex.Message,
                SyncedAt = DateTime.UtcNow
            };
        }
    }

    // Helper methods

    private void SetSquareApiHeaders(string accessToken)
    {
        // Clear existing headers to avoid duplicates
        _httpClient.DefaultRequestHeaders.Authorization = null;
        _httpClient.DefaultRequestHeaders.Remove("Square-Version");

        // Set fresh headers
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        _httpClient.DefaultRequestHeaders.Add("Square-Version", "2023-10-18");
    }

    private async Task<SquareConnection?> GetSquareConnectionAsync(Guid shopId)
    {
        _logger.LogInformation("Getting Square connection for organization {ShopId}", shopId);

        // Query SquareConnection directly to ensure it's loaded
        var squareConnection = await _unitOfWork.SquareConnections.GetAsync(sc => sc.OrganizationId == shopId);

        _logger.LogInformation("Square connection found: {ConnectionFound}", squareConnection != null);

        if (squareConnection != null)
        {
            _logger.LogInformation("Square connection details - IsConnected: {IsConnected}, LocationId: {LocationId}, HasToken: {HasToken}",
                squareConnection.IsConnected,
                squareConnection.LocationId,
                !string.IsNullOrEmpty(squareConnection.AccessToken));
        }

        return squareConnection;
    }

    private async Task<UpsertResult> UpsertItemFromSquare(Guid shopId, SquareCatalogObject item)
    {
        _logger.LogInformation("🔍 [UpsertItemFromSquare] START - Processing Square item {SquareCatalogId} for organization {ShopId}", item.Id, shopId);

        // Log raw Square data for debugging category issues
        _logger.LogInformation("📋 [SQUARE_RAW_DATA] Item JSON: {RawItem}", JsonSerializer.Serialize(item, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower, WriteIndented = true }));

        var variation = item.ItemData?.Variations?.FirstOrDefault();
        if (variation == null)
        {
            throw new Exception("Item has no variations");
        }

        _logger.LogInformation("🔍 [UpsertItemFromSquare] Item details - Name: {ItemName}, SquareCatalogId: {SquareCatalogId}, VariationId: {VariationId}",
            item.ItemData?.Name ?? "Unknown", item.Id, variation.Id);

        // Get category name if available
        string? categoryName = null;
        _logger.LogInformation("🔍 [UpsertItemFromSquare] Item CategoryId: '{CategoryId}' (IsNullOrWhiteSpace: {IsNull})",
            item.ItemData?.CategoryId ?? "NULL", string.IsNullOrWhiteSpace(item.ItemData?.CategoryId));

        if (!string.IsNullOrWhiteSpace(item.ItemData?.CategoryId))
        {
            // First, try to get the Square category object to get its name
            _logger.LogInformation("Item has CategoryId: {CategoryId}, looking up Square category...", item.ItemData.CategoryId);

            try
            {
                SetSquareApiHeaders((await GetSquareConnectionAsync(shopId))?.AccessToken ?? "");
                var categoryUrl = $"{_squareBaseUrl}/v2/catalog/object/{item.ItemData.CategoryId}";
                var categoryResponse = await _httpClient.GetAsync(categoryUrl);

                if (categoryResponse.IsSuccessStatusCode)
                {
                    var categoryContent = await categoryResponse.Content.ReadAsStringAsync();
                    var categoryObject = JsonSerializer.Deserialize<SquareCatalogObjectResponse>(categoryContent, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                    });

                    if (categoryObject?.Object?.CategoryData?.Name != null)
                    {
                        categoryName = categoryObject.Object.CategoryData.Name;
                        _logger.LogInformation("Found Square category name: {CategoryName}", categoryName);

                        // Ensure this category exists in our system (try Square ID first, then name)
                        var existingCategory = await _unitOfWork.ItemCategories.GetAsync(c =>
                            c.OrganizationId == shopId &&
                            c.SquareCategoryId == item.ItemData.CategoryId);

                        if (existingCategory == null)
                        {
                            existingCategory = await _unitOfWork.ItemCategories.GetAsync(c =>
                                c.OrganizationId == shopId &&
                                c.Name == categoryName);
                        }

                        if (existingCategory == null)
                        {
                            _logger.LogInformation("Category {CategoryName} (Square ID: {SquareId}) doesn't exist in CG, creating it...", categoryName, item.ItemData.CategoryId);
                            var newCategory = new ItemCategory
                            {
                                Id = Guid.NewGuid(),
                                OrganizationId = shopId,
                                Name = categoryName,
                                SquareCategoryId = item.ItemData.CategoryId, // Store the Square category ID
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };
                            await _unitOfWork.ItemCategories.AddAsync(newCategory);
                            await _unitOfWork.SaveChangesAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to lookup Square category {CategoryId}", item.ItemData.CategoryId);
            }
        }

        // Check if already in Items table (previously assigned)
        _logger.LogInformation("🔍 [UpsertItemFromSquare] Checking if item exists in main Items table...");
        var existingItem = await _unitOfWork.Items.GetAsync(i =>
            i.OrganizationId == shopId &&
            i.SquareCatalogId == item.Id);

        if (existingItem != null)
        {
            _logger.LogInformation("✅ [UpsertItemFromSquare] Found existing item in Items table - ItemId: {ItemId}, updating...", existingItem.Id);
            // EXISTING ITEM - update price/name, preserve consignor & status
            existingItem.Title = item.ItemData?.Name ?? existingItem.Title;
            existingItem.Price = (variation.ItemVariationData?.PriceMoney?.Amount ?? 0) / 100m;
            existingItem.SquareLastSyncAt = DateTime.UtcNow;
            existingItem.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Items.UpdateAsync(existingItem);

            _logger.LogInformation("✅ [UpsertItemFromSquare] Updated existing item in Items table");
            return new UpsertResult { Created = false, CgItemId = existingItem.Id, CategoryName = categoryName };
        }

        // Use unified pending import system instead of old PendingSquareImports table
        _logger.LogInformation("🔍 [UpsertItemFromSquare] Item not in Items table, using unified pending import system...");

        _logger.LogInformation("🔄 [SquareSync->PendingImport] CALLING SERVICE - SquareCatalogId: {CatalogId}, VariationId: {VariationId}, Name: {Name}, OrgId: {OrgId}, Price: {Price}",
            item.Id, variation.Id, item.ItemData?.Name ?? "Unknown Item", shopId, (variation.ItemVariationData?.PriceMoney?.Amount ?? 0) / 100m);

        var squareRequest = new CreatePendingImportFromSquareRequest
        {
            SquareCatalogId = item.Id,
            SquareVariationId = variation.Id,
            Name = item.ItemData?.Name ?? "Unknown Item",
            Description = item.ItemData?.Description,
            Price = (variation.ItemVariationData?.PriceMoney?.Amount ?? 0) / 100m,
            Sku = variation.ItemVariationData?.Sku ?? $"SQUARE-{item.Id[..8]}",
            Category = categoryName,
            Condition = "Good", // Default condition for Square items
            SquareUpdatedAt = DateTime.UtcNow,
            Notes = null
        };

        var pendingImport = await _pendingImportItemService.CreateFromSquareAsync(shopId, squareRequest);
        _logger.LogInformation("✅ [SquareSync->PendingImport] SERVICE COMPLETED - PendingImportId: {PendingId}, Name: {Name}, CatalogId: {CatalogId}",
            pendingImport.Id, pendingImport.Name, item.Id);
        _logger.LogInformation("✅ [UpsertItemFromSquare] Created/updated unified pending import with ID: {PendingId}", pendingImport.Id);

        _logger.LogInformation("✅ [UpsertItemFromSquare] END - Completed processing for Square item {SquareCatalogId}", item.Id);
        return new UpsertResult { Created = true, CgItemId = pendingImport.Id, CategoryName = categoryName };
    }

    public async Task<TransactionSyncResult> SyncTransactionsAsync(Guid shopId)
    {
        _logger.LogInformation("Starting Square transactions sync for organization {ShopId}", shopId);

        var result = new TransactionSyncResult
        {
            SyncedAt = DateTime.UtcNow
        };

        try
        {
            var connection = await GetSquareConnectionAsync(shopId);
            if (connection == null)
            {
                throw new InvalidOperationException("Square connection not found");
            }

            SetSquareApiHeaders(connection.AccessToken);

            // Get transactions from last 30 days
            var startTime = DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-ddTHH:mm:ssZ");
            var endTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

            var url = $"{_squareBaseUrl}/v2/payments?location_id={connection.LocationId}&begin_time={startTime}&end_time={endTime}";
            _logger.LogInformation("Square Payments API call: {Url}", url);

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to fetch Square payments: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var paymentsResponse = JsonSerializer.Deserialize<SquarePaymentsResponse>(content, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            var payments = paymentsResponse?.Payments ?? new List<SquarePayment>();
            _logger.LogInformation("Retrieved {PaymentCount} payments from Square", payments.Count);

            // Debug log the response
            if (payments.Count == 0)
            {
                _logger.LogWarning("No payments found from Square API for date range {StartTime} to {EndTime}. " +
                    "Check if there are actual transactions in Square for this period.", startTime, endTime);
            }

            int pendingCreated = 0, mainCreated = 0;

            foreach (var payment in payments)
            {
                try
                {
                    var processed = await ProcessSquarePayment(shopId, payment);
                    if (processed.CreatedPending) pendingCreated++;
                    if (processed.CreatedMain) mainCreated++;
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Payment {payment.Id}: {ex.Message}");
                    _logger.LogWarning(ex, "Failed to process Square payment {PaymentId}", payment.Id);
                }
            }

            result.Success = result.Errors.Count == 0;
            result.TransactionsSynced = payments.Count;
            result.PendingTransactionsCreated = pendingCreated;
            result.MainTransactionsCreated = mainCreated;

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Square transactions sync completed for organization {ShopId}. " +
                "Synced: {TransactionsSynced}, Pending: {PendingCreated}, Main: {MainCreated}",
                shopId, result.TransactionsSynced, result.PendingTransactionsCreated, result.MainTransactionsCreated);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Square transactions sync for organization {ShopId}", shopId);
            result.Success = false;
            result.Error = ex.Message;
            return result;
        }
    }

    private async Task<TransactionProcessResult> ProcessSquarePayment(Guid shopId, SquarePayment payment)
    {
        var result = new TransactionProcessResult();

        // Skip if payment is not completed
        if (payment.Status != "COMPLETED")
        {
            _logger.LogDebug("Skipping payment {PaymentId} with status {Status}", payment.Id, payment.Status);
            return result;
        }

        // Check if already processed
        var existingTransaction = await _unitOfWork.Transactions.GetAsync(t =>
            t.OrganizationId == shopId && t.SquarePaymentId == payment.Id);

        if (existingTransaction != null)
        {
            _logger.LogDebug("Payment {PaymentId} already exists as transaction {TransactionId}", payment.Id, existingTransaction.Id);
            return result;
        }

        var existingPending = await _unitOfWork.PendingSquareTransactions.GetAsync(pt =>
            pt.OrganizationId == shopId && pt.SquarePaymentId == payment.Id);

        if (existingPending != null)
        {
            _logger.LogDebug("Payment {PaymentId} already exists as pending transaction {TransactionId}", payment.Id, existingPending.Id);
            return result;
        }

        // Get order details to find items
        if (string.IsNullOrEmpty(payment.OrderId))
        {
            _logger.LogWarning("Payment {PaymentId} has no associated order - skipping", payment.Id);
            return result;
        }

        _logger.LogDebug("Fetching order {OrderId} for payment {PaymentId}", payment.OrderId, payment.Id);
        var order = await GetSquareOrderAsync(payment.OrderId);
        if (order == null || order.LineItems == null || !order.LineItems.Any())
        {
            _logger.LogWarning("Order {OrderId} not found or has no line items - skipping payment {PaymentId}", payment.OrderId, payment.Id);
            return result;
        }

        _logger.LogDebug("Order {OrderId} has {ItemCount} line items", payment.OrderId, order.LineItems.Count);

        // Check if all items are assigned or pending
        bool allItemsAssigned = true;
        var transactionItems = new List<TransactionItemData>();

        foreach (var lineItem in order.LineItems)
        {
            if (string.IsNullOrEmpty(lineItem.CatalogObjectId))
                continue;

            // Check if item is assigned (exists in Items table)
            var assignedItem = await _unitOfWork.Items.GetAsync(i =>
                i.OrganizationId == shopId && i.SquareCatalogId == lineItem.CatalogObjectId);

            if (assignedItem != null)
            {
                transactionItems.Add(new TransactionItemData
                {
                    ItemId = assignedItem.Id,
                    ConsignorId = assignedItem.ConsignorId,
                    Quantity = lineItem.Quantity?.ToInt() ?? 1,
                    UnitPrice = (lineItem.VariationTotalPriceMoney?.Amount ?? 0) / 100m,
                    IsAssigned = true,
                    SquareCatalogId = lineItem.CatalogObjectId,
                    ItemName = lineItem.Name ?? "Unknown Item"
                });
            }
            else
            {
                // Item is pending - check if it exists in pending table (using old table for transaction processing for now)
                var pendingItem = await _unitOfWork.PendingSquareImports.GetAsync(p =>
                    p.OrganizationId == shopId && p.SquareCatalogId == lineItem.CatalogObjectId);

                transactionItems.Add(new TransactionItemData
                {
                    PendingSquareImportId = pendingItem?.Id,
                    Quantity = lineItem.Quantity?.ToInt() ?? 1,
                    UnitPrice = (lineItem.VariationTotalPriceMoney?.Amount ?? 0) / 100m,
                    IsAssigned = false,
                    SquareCatalogId = lineItem.CatalogObjectId,
                    ItemName = lineItem.Name ?? "Unknown Item"
                });
                allItemsAssigned = false;
            }
        }

        if (allItemsAssigned)
        {
            // Create main transaction
            await CreateMainTransaction(shopId, payment, order, transactionItems);
            result.CreatedMain = true;
        }
        else
        {
            // Create pending transaction
            await CreatePendingTransaction(shopId, payment, order, transactionItems);
            result.CreatedPending = true;
        }

        return result;
    }

    private async Task<SquareOrder?> GetSquareOrderAsync(string orderId)
    {
        try
        {
            var url = $"{_squareBaseUrl}/v2/orders/{orderId}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync();
            var orderResponse = JsonSerializer.Deserialize<SquareOrderResponse>(content, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            return orderResponse?.Order;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get Square order {OrderId}", orderId);
            return null;
        }
    }

    private async Task CreateMainTransaction(Guid shopId, SquarePayment payment, SquareOrder order, List<TransactionItemData> items)
    {
        var transaction = new Core.Entities.Transaction
        {
            Id = Guid.NewGuid(),
            OrganizationId = shopId,
            TransactionDate = DateTime.Parse(payment.CreatedAt ?? DateTime.UtcNow.ToString()),
            Source = "Square",
            PaymentType = "Credit Card", // Simplified
            Subtotal = (order.TotalMoney?.Amount ?? 0) / 100m,
            TaxAmount = (order.TotalTaxMoney?.Amount ?? 0) / 100m,
            Total = (payment.AmountMoney?.Amount ?? 0) / 100m,
            SquarePaymentId = payment.Id,
            SquareLocationId = payment.LocationId,
            ImportedFromSquare = true,
            SquareCreatedAt = DateTime.Parse(payment.CreatedAt ?? DateTime.UtcNow.ToString()),
            Status = "Completed",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Transactions.AddAsync(transaction);

        foreach (var itemData in items.Where(i => i.IsAssigned))
        {
            var transactionItem = new Core.Entities.TransactionItem
            {
                Id = Guid.NewGuid(),
                TransactionId = transaction.Id,
                OrganizationId = shopId,
                ItemId = itemData.ItemId!.Value,
                ConsignorId = itemData.ConsignorId!.Value,
                Quantity = itemData.Quantity,
                UnitPrice = itemData.UnitPrice,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Get consignor for split calculation
            var consignor = await _unitOfWork.Consignors.GetByIdAsync(itemData.ConsignorId.Value);
            if (consignor != null)
            {
                var lineTotal = itemData.UnitPrice * itemData.Quantity;
                transactionItem.ConsignorSplitPercentage = consignor.CommissionRate;
                transactionItem.ConsignorAmount = lineTotal * consignor.CommissionRate;
                transactionItem.StoreAmount = lineTotal * (1 - consignor.CommissionRate);
            }

            await _unitOfWork.TransactionItems.AddAsync(transactionItem);

            // Update item status to Sold
            var item = await _unitOfWork.Items.GetByIdAsync(itemData.ItemId.Value);
            if (item != null)
            {
                item.Status = Core.Enums.ItemStatus.Sold;
                item.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.Items.UpdateAsync(item);
            }
        }
    }

    private async Task CreatePendingTransaction(Guid shopId, SquarePayment payment, SquareOrder order, List<TransactionItemData> items)
    {
        var pendingTransaction = new Core.Entities.PendingSquareTransaction
        {
            Id = Guid.NewGuid(),
            OrganizationId = shopId,
            TransactionDate = DateTime.Parse(payment.CreatedAt ?? DateTime.UtcNow.ToString()),
            Source = "Square",
            PaymentType = "Credit Card",
            Subtotal = (order.TotalMoney?.Amount ?? 0) / 100m,
            TaxAmount = (order.TotalTaxMoney?.Amount ?? 0) / 100m,
            Total = (payment.AmountMoney?.Amount ?? 0) / 100m,
            SquarePaymentId = payment.Id,
            SquareLocationId = payment.LocationId,
            SquareCreatedAt = DateTime.Parse(payment.CreatedAt ?? DateTime.UtcNow.ToString()),
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.PendingSquareTransactions.AddAsync(pendingTransaction);

        foreach (var itemData in items)
        {
            var pendingItem = new Core.Entities.PendingSquareTransactionItem
            {
                Id = Guid.NewGuid(),
                PendingTransactionId = pendingTransaction.Id,
                OrganizationId = shopId,
                PendingSquareImportId = itemData.PendingSquareImportId,
                ItemId = itemData.ItemId,
                ConsignorId = itemData.ConsignorId,
                SquareCatalogId = itemData.SquareCatalogId,
                Quantity = itemData.Quantity,
                UnitPrice = itemData.UnitPrice,
                ItemName = itemData.ItemName,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Calculate splits if item is assigned
            if (itemData.IsAssigned && itemData.ConsignorId.HasValue)
            {
                var consignor = await _unitOfWork.Consignors.GetByIdAsync(itemData.ConsignorId.Value);
                if (consignor != null)
                {
                    var lineTotal = itemData.UnitPrice * itemData.Quantity;
                    pendingItem.ConsignorSplitPercentage = consignor.CommissionRate;
                    pendingItem.ConsignorAmount = lineTotal * consignor.CommissionRate;
                    pendingItem.StoreAmount = lineTotal * (1 - consignor.CommissionRate);
                }
            }

            await _unitOfWork.PendingSquareTransactionItems.AddAsync(pendingItem);
        }
    }

    // TODO: Implement Square sync completion notifications
    // private async Task SendSyncCompletionNotificationAsync(...)

    // Helper classes (keeping existing Square API models for now)
    private class UpsertResult
    {
        public bool Created { get; set; }
        public Guid CgItemId { get; set; }
        public string? CategoryName { get; set; }
    }

    private class TransactionProcessResult
    {
        public bool CreatedPending { get; set; }
        public bool CreatedMain { get; set; }
    }

    private class TransactionItemData
    {
        public Guid? ItemId { get; set; }
        public Guid? ConsignorId { get; set; }
        public Guid? PendingSquareImportId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public bool IsAssigned { get; set; }
        public string SquareCatalogId { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
    }
}

// Square API response models (reusing existing)
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

// Payment API models
public class SquarePaymentsResponse
{
    public List<SquarePayment>? Payments { get; set; }
    public string? Cursor { get; set; }
}

public class SquarePayment
{
    public string Id { get; set; } = string.Empty;
    public string? CreatedAt { get; set; }
    public string? UpdatedAt { get; set; }
    public SquarePriceMoney? AmountMoney { get; set; }
    public string? Status { get; set; }
    public string? LocationId { get; set; }
    public string? OrderId { get; set; }
}

// Order API models
public class SquareOrderResponse
{
    public SquareOrder? Order { get; set; }
}

public class SquareOrder
{
    public string Id { get; set; } = string.Empty;
    public string? LocationId { get; set; }
    public List<SquareOrderLineItem>? LineItems { get; set; }
    public SquarePriceMoney? TotalMoney { get; set; }
    public SquarePriceMoney? TotalTaxMoney { get; set; }
    public string? CreatedAt { get; set; }
}

public class SquareOrderLineItem
{
    public string? Name { get; set; }
    public string? Quantity { get; set; }
    public string? CatalogObjectId { get; set; }
    public SquarePriceMoney? VariationTotalPriceMoney { get; set; }
}

// Extension method for Square quantity parsing
public static class SquareExtensions
{
    public static int ToInt(this string? quantity)
    {
        if (string.IsNullOrEmpty(quantity))
            return 1;

        if (int.TryParse(quantity, out var result))
            return result;

        return 1;
    }
}