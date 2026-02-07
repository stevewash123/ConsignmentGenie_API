using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Core.Interfaces;
using ConsignmentGenie.Core.DTOs.Notifications;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace ConsignmentGenie.Application.Services;

public interface IPendingImportItemService
{
    Task<PagedResult<PendingImportItemDto>> GetPendingImportItemsAsync(Guid organizationId, PendingImportItemsQueryParams queryParams);
    Task<PendingImportItemDto?> GetPendingImportItemAsync(Guid organizationId, Guid id);
    Task<PendingImportItemDto> CreatePendingImportItemAsync(Guid organizationId, CreatePendingImportItemRequest request);
    Task<List<PendingImportItemDto>> CreatePendingImportItemsBulkAsync(Guid organizationId, List<CreatePendingImportItemRequest> requests);
    Task<PendingImportItemDto?> UpdatePendingImportItemAsync(Guid organizationId, Guid id, UpdatePendingImportItemRequest request);
    Task<PendingImportItemDto?> PatchPendingImportItemAsync(Guid organizationId, Guid id, PatchPendingImportItemRequest request);
    Task<bool> DeletePendingImportItemAsync(Guid organizationId, Guid id, Guid currentUserId);
    Task<int> DeletePendingImportItemsBulkAsync(Guid organizationId, List<Guid> ids);
    Task<BulkAssignPendingImportsResult> BulkAssignConsignorAsync(Guid organizationId, BulkAssignPendingImportsRequest request);
    Task<PendingImportItemDto?> AssignConsignorAsync(Guid organizationId, Guid id, Guid consignorId);
    Task<ImportPendingItemsResult> ImportVerifiedItemsAsync(Guid organizationId, ImportVerifiedItemsRequest request);
    Task<List<PendingImportItemDto>> CreateFromManifestAsync(Guid organizationId, CreatePendingImportsFromManifestRequest request);
    Task<List<PendingImportItemDto>> CreateFromCsvAsync(Guid organizationId, CreatePendingImportsFromCsvRequest request);
    Task<PendingImportItemDto> CreateFromSquareAsync(Guid organizationId, CreatePendingImportFromSquareRequest request);
}

public class PendingImportItemService : IPendingImportItemService
{
    private readonly ConsignmentGenieContext _context;
    private readonly ILogger<PendingImportItemService> _logger;
    private readonly ISquareInventoryService _squareInventoryService;
    private readonly IConsignorNotificationService _notificationService;
    private readonly ConsolidatedNotificationService _consolidatedNotificationService;

    public PendingImportItemService(
        ConsignmentGenieContext context,
        ILogger<PendingImportItemService> logger,
        ISquareInventoryService squareInventoryService,
        IConsignorNotificationService notificationService,
        ConsolidatedNotificationService consolidatedNotificationService)
    {
        _context = context;
        _logger = logger;
        _squareInventoryService = squareInventoryService;
        _notificationService = notificationService;
        _consolidatedNotificationService = consolidatedNotificationService;
    }

    public async Task<PagedResult<PendingImportItemDto>> GetPendingImportItemsAsync(Guid organizationId, PendingImportItemsQueryParams queryParams)
    {
        var query = _context.PendingImportItems
            .Include(pii => pii.Consignor)
            .Where(pii => pii.OrganizationId == organizationId && pii.Status != ImportStatus.Deleted);

        // Apply filters
        if (!string.IsNullOrWhiteSpace(queryParams.Search))
        {
            var searchTerm = queryParams.Search.ToLower();
            query = query.Where(pii =>
                pii.Name.ToLower().Contains(searchTerm) ||
                (pii.Description != null && pii.Description.ToLower().Contains(searchTerm)) ||
                (pii.Sku != null && pii.Sku.ToLower().Contains(searchTerm)));
        }

        if (queryParams.Source.HasValue)
        {
            query = query.Where(pii => pii.Source == queryParams.Source.Value);
        }

        if (queryParams.Status.HasValue)
        {
            query = query.Where(pii => pii.Status == queryParams.Status.Value);
        }

        if (queryParams.ConsignorId.HasValue)
        {
            query = query.Where(pii => pii.ConsignorId == queryParams.ConsignorId.Value);
        }

        if (!string.IsNullOrWhiteSpace(queryParams.SourceReference))
        {
            query = query.Where(pii => pii.SourceReference == queryParams.SourceReference);
        }

        // Category filtering
        if (!string.IsNullOrWhiteSpace(queryParams.Category))
        {
            query = query.Where(pii => pii.Category == queryParams.Category);
        }

        // Condition filtering
        if (!string.IsNullOrWhiteSpace(queryParams.Condition))
        {
            query = query.Where(pii => pii.Condition == queryParams.Condition);
        }

        // Apply sorting
        query = ApplySorting(query, queryParams.SortBy ?? "CreatedAt", queryParams.SortDirection ?? "desc");

        // Get total count
        var totalCount = await query.CountAsync();

        // Apply paging
        var items = await query
            .Skip((queryParams.Page - 1) * queryParams.PageSize)
            .Take(queryParams.PageSize)
            .ToListAsync();

        var dtos = items.Select(MapToPendingImportItemDto).ToList();

        return new PagedResult<PendingImportItemDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = queryParams.Page,
            PageSize = queryParams.PageSize
        };
    }

    public async Task<PendingImportItemDto?> GetPendingImportItemAsync(Guid organizationId, Guid id)
    {
        var item = await _context.PendingImportItems
            .Include(pii => pii.Consignor)
            .FirstOrDefaultAsync(pii => pii.Id == id && pii.OrganizationId == organizationId && pii.Status != ImportStatus.Deleted);

        return item != null ? MapToPendingImportItemDto(item) : null;
    }

    public async Task<PendingImportItemDto> CreatePendingImportItemAsync(Guid organizationId, CreatePendingImportItemRequest request)
    {
        var entity = new PendingImportItem
        {
            OrganizationId = organizationId,
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            MinimumPrice = request.MinimumPrice,
            Sku = request.Sku,
            Category = request.Category,
            Brand = request.Brand,
            Condition = request.Condition,
            ImageUrl = request.ImageUrl,
            Source = request.Source,
            SourceReference = request.SourceReference,
            ConsignorId = request.ConsignorId,
            Notes = request.Notes,
            Status = ImportStatus.Pending
        };

        _logger.LogInformation("🔄 [PendingImportWrite] SINGLE ADD - Name: {Name}, Source: {Source}, SourceRef: {SourceRef}, OrgId: {OrgId}, ConsignorId: {ConsignorId}, Price: {Price}, CreatedAt: {CreatedAt}",
            request.Name, request.Source, request.SourceReference, organizationId, request.ConsignorId, request.Price, DateTime.UtcNow);

        _context.PendingImportItems.Add(entity);
        await _context.SaveChangesAsync();

        _logger.LogInformation("✅ [PendingImportWrite] SINGLE ADD COMPLETED - ID: {EntityId}, Name: {Name}", entity.Id, entity.Name);

        return await GetPendingImportItemAsync(organizationId, entity.Id)
            ?? throw new InvalidOperationException("Failed to retrieve created item");
    }

    public async Task<List<PendingImportItemDto>> CreatePendingImportItemsBulkAsync(Guid organizationId, List<CreatePendingImportItemRequest> requests)
    {
        _logger.LogInformation("🔄 [PendingImportWrite] BULK ADD START - Count: {Count}, OrgId: {OrgId}, Sources: [{Sources}]",
            requests.Count, organizationId, string.Join(", ", requests.Select(r => r.Source).Distinct()));

        var entities = requests.Select(request => new PendingImportItem
        {
            OrganizationId = organizationId,
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            MinimumPrice = request.MinimumPrice,
            Sku = request.Sku,
            Category = request.Category,
            Brand = request.Brand,
            Condition = request.Condition,
            ImageUrl = request.ImageUrl,
            Source = request.Source,
            SourceReference = request.SourceReference,
            ConsignorId = request.ConsignorId,
            Notes = request.Notes,
            Status = ImportStatus.Pending
        }).ToList();

        foreach (var entity in entities)
        {
            _logger.LogInformation("📝 [PendingImportWrite] BULK ITEM - Name: {Name}, Source: {Source}, SourceRef: {SourceRef}, ConsignorId: {ConsignorId}, Price: {Price}",
                entity.Name, entity.Source, entity.SourceReference, entity.ConsignorId, entity.Price);
        }

        _context.PendingImportItems.AddRange(entities);
        await _context.SaveChangesAsync();

        _logger.LogInformation("✅ [PendingImportWrite] BULK ADD COMPLETED - Count: {Count}, IDs: [{EntityIds}]",
            entities.Count, string.Join(", ", entities.Select(e => e.Id)));

        var ids = entities.Select(e => e.Id).ToList();
        var items = await _context.PendingImportItems
            .Include(pii => pii.Consignor)
            .Where(pii => ids.Contains(pii.Id))
            .ToListAsync();

        return items.Select(MapToPendingImportItemDto).ToList();
    }

    public async Task<PendingImportItemDto?> UpdatePendingImportItemAsync(Guid organizationId, Guid id, UpdatePendingImportItemRequest request)
    {
        var entity = await _context.PendingImportItems
            .FirstOrDefaultAsync(pii => pii.Id == id && pii.OrganizationId == organizationId);

        if (entity == null) return null;

        if (!string.IsNullOrEmpty(request.Name)) entity.Name = request.Name;
        if (request.Description != null) entity.Description = request.Description;
        if (request.Price.HasValue) entity.Price = request.Price.Value;
        if (request.Sku != null) entity.Sku = request.Sku;
        if (request.Category != null) entity.Category = request.Category;
        if (request.Condition != null) entity.Condition = request.Condition;
        if (request.ConsignorId.HasValue) entity.ConsignorId = request.ConsignorId.Value;
        if (request.Notes != null) entity.Notes = request.Notes;

        await _context.SaveChangesAsync();

        return await GetPendingImportItemAsync(organizationId, id);
    }

    public async Task<PendingImportItemDto?> PatchPendingImportItemAsync(Guid organizationId, Guid id, PatchPendingImportItemRequest request)
    {
        var entity = await _context.PendingImportItems
            .FirstOrDefaultAsync(pii => pii.Id == id && pii.OrganizationId == organizationId);

        if (entity == null) return null;

        // Only update provided fields (true PATCH semantics)
        if (request.Price.HasValue) entity.Price = request.Price.Value;
        if (request.Category != null) entity.Category = request.Category;
        if (request.Condition != null) entity.Condition = request.Condition;

        await _context.SaveChangesAsync();

        return await GetPendingImportItemAsync(organizationId, id);
    }

    public async Task<bool> DeletePendingImportItemAsync(Guid organizationId, Guid id, Guid currentUserId)
    {
        var entity = await _context.PendingImportItems
            .Include(pii => pii.Consignor)
            .FirstOrDefaultAsync(pii => pii.Id == id && pii.OrganizationId == organizationId);

        if (entity == null) return false;

        // Implement soft delete by setting status to Deleted
        entity.Status = ImportStatus.Deleted;
        await _context.SaveChangesAsync();

        // Send rejection notification to consignor if assigned
        if (entity.ConsignorId.HasValue && entity.Consignor != null)
        {
            try
            {
                await _notificationService.CreateNotificationAsync(new CreateNotificationRequest
                {
                    OrganizationId = organizationId,
                    FromUserId = currentUserId,
                    FromType = "owner",
                    ToUserId = entity.ConsignorId.Value,
                    ToType = "consignor",
                    Type = NotificationType.ItemRejected.ToString(),
                    Title = "Item Rejected",
                    Message = $"Your item '{entity.Name}' has been rejected and will not be processed for consignment.",
                    Payload = new
                    {
                        ItemName = entity.Name,
                        ItemDescription = entity.Description,
                        ItemPrice = entity.Price,
                        RejectionDate = DateTime.UtcNow,
                        Reason = "Item did not meet consignment criteria"
                    },
                    ReferenceType = "pending_import",
                    ReferenceId = entity.Id
                });

                _logger.LogInformation("Rejection notification sent to consignor {ConsignorId} for item {ItemName}",
                    entity.ConsignorId, entity.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send rejection notification to consignor {ConsignorId} for item {ItemName}",
                    entity.ConsignorId, entity.Name);
                // Don't fail the deletion if notification fails
            }
        }

        return true;
    }

    public async Task<int> DeletePendingImportItemsBulkAsync(Guid organizationId, List<Guid> ids)
    {
        var entities = await _context.PendingImportItems
            .Include(pii => pii.Consignor)
            .Where(pii => ids.Contains(pii.Id) && pii.OrganizationId == organizationId)
            .ToListAsync();

        // Implement soft delete for all entities
        foreach (var entity in entities)
        {
            entity.Status = ImportStatus.Deleted;
        }

        await _context.SaveChangesAsync();

        // Send rejection notifications for items with assigned consignors
        var entitiesWithConsignors = entities.Where(e => e.ConsignorId.HasValue && e.Consignor != null).ToList();

        foreach (var entity in entitiesWithConsignors)
        {
            try
            {
                await _notificationService.CreateNotificationAsync(new CreateNotificationRequest
                {
                    OrganizationId = organizationId,
                    FromType = "system",
                    ToUserId = entity.ConsignorId!.Value,
                    ToType = "consignor",
                    Type = NotificationType.ItemRejected.ToString(),
                    Title = "Item Rejected",
                    Message = $"Your item '{entity.Name}' has been rejected and will not be processed for consignment.",
                    Payload = new
                    {
                        ItemName = entity.Name,
                        ItemDescription = entity.Description,
                        ItemPrice = entity.Price,
                        RejectionDate = DateTime.UtcNow,
                        Reason = "Item did not meet consignment criteria"
                    },
                    ReferenceType = "pending_import",
                    ReferenceId = entity.Id
                });

                _logger.LogInformation("Bulk rejection notification sent to consignor {ConsignorId} for item {ItemName}",
                    entity.ConsignorId, entity.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send bulk rejection notification to consignor {ConsignorId} for item {ItemName}",
                    entity.ConsignorId, entity.Name);
                // Don't fail the deletion if notification fails
            }
        }

        return entities.Count;
    }

    public async Task<BulkAssignPendingImportsResult> BulkAssignConsignorAsync(Guid organizationId, BulkAssignPendingImportsRequest request)
    {
        var result = new BulkAssignPendingImportsResult();

        // Verify consignor exists and belongs to organization
        var consignor = await _context.Consignors
            .FirstOrDefaultAsync(c => c.Id == request.ConsignorId && c.OrganizationId == organizationId);

        if (consignor == null)
        {
            result.Failed.AddRange(request.PendingImportIds.Select(id => new FailedPendingImportAssignmentDto
            {
                PendingImportId = id,
                Reason = "Consignor not found or does not belong to organization"
            }));
            return result;
        }

        var items = await _context.PendingImportItems
            .Include(pii => pii.Consignor)
            .Where(pii => request.PendingImportIds.Contains(pii.Id) && pii.OrganizationId == organizationId)
            .ToListAsync();

        foreach (var item in items)
        {
            try
            {
                item.ConsignorId = request.ConsignorId;
                // Set status based on whether markAsVerified was requested
                item.Status = request.MarkAsVerified ? ImportStatus.Verified : ImportStatus.Assigned;
                result.Assigned++;
            }
            catch (Exception ex)
            {
                result.Failed.Add(new FailedPendingImportAssignmentDto
                {
                    PendingImportId = item.Id,
                    Reason = ex.Message
                });
            }
        }

        await _context.SaveChangesAsync();

        // Reload the entities with consignor data and map to DTOs
        foreach (var item in items.Where(i => !result.Failed.Any(f => f.PendingImportId == i.Id)))
        {
            await _context.Entry(item).Reference(e => e.Consignor).LoadAsync();
            result.UpdatedItems.Add(MapToPendingImportItemDto(item));
        }

        return result;
    }

    public async Task<PendingImportItemDto?> AssignConsignorAsync(Guid organizationId, Guid id, Guid consignorId)
    {
        var entity = await _context.PendingImportItems
            .Include(pii => pii.Consignor)
            .FirstOrDefaultAsync(pii => pii.Id == id && pii.OrganizationId == organizationId);

        if (entity == null) return null;

        // Verify consignor exists and belongs to organization
        var consignorExists = await _context.Consignors
            .AnyAsync(c => c.Id == consignorId && c.OrganizationId == organizationId);

        if (!consignorExists) return null;

        entity.ConsignorId = consignorId;
        entity.Status = ImportStatus.Assigned;

        await _context.SaveChangesAsync();

        // Reload the entity with the consignor data to ensure proper mapping
        await _context.Entry(entity).Reference(e => e.Consignor).LoadAsync();

        return MapToPendingImportItemDto(entity);
    }

    public async Task<ImportPendingItemsResult> ImportVerifiedItemsAsync(Guid organizationId, ImportVerifiedItemsRequest request)
    {
        var result = new ImportPendingItemsResult();

        var items = await _context.PendingImportItems
            .Include(pii => pii.Consignor)
            .Where(pii => request.PendingImportIds.Contains(pii.Id) &&
                         pii.OrganizationId == organizationId &&
                         pii.ConsignorId != null)
            .ToListAsync();

        foreach (var pendingItem in items)
        {
            try
            {
                // Create the actual Item from pending import
                var sku = pendingItem.Sku;
                if (string.IsNullOrWhiteSpace(sku))
                {
                    // Generate a unique SKU if not provided
                    sku = $"IMP-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
                }

                // Look up or create category if category name is provided
                Guid? categoryId = await GetOrCreateCategoryIdAsync(pendingItem.Category, organizationId);

                var item = new Item
                {
                    OrganizationId = organizationId,
                    ConsignorId = pendingItem.ConsignorId!.Value,
                    Title = pendingItem.Name,
                    Description = pendingItem.Description,
                    Price = pendingItem.Price,
                    MinimumPrice = pendingItem.MinimumPrice,
                    Sku = sku,
                    ItemCategoryId = categoryId,
                    Condition = ParseItemCondition(pendingItem.Condition),
                    Status = ItemStatus.Available,
                    ReceivedDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    Notes = pendingItem.Notes,
                    PrimaryImageUrl = pendingItem.ImageUrl
                };

                _context.Items.Add(item);
                await _context.SaveChangesAsync(); // Save to get the ID

                // Mark for deletion after successful import
                _context.PendingImportItems.Remove(pendingItem);

                result.Imported++;
                result.ImportedItemIds.Add(item.Id);

                _logger.LogInformation("✅ [ImportVerified] Successfully imported and deleted pending import {PendingImportId} -> Item {ItemId}",
                    pendingItem.Id, item.Id);

                // Sync the newly created item to Square (if Square integration is configured)
                try
                {
                    await _squareInventoryService.SyncItemAsync(organizationId.ToString(), item.Id.ToString());
                    _logger.LogInformation("Successfully synced imported item {ItemId} to Square", item.Id);
                }
                catch (Exception syncEx)
                {
                    _logger.LogWarning(syncEx, "Failed to sync imported item {ItemId} to Square, but item was successfully imported", item.Id);
                    // Don't fail the entire import if Square sync fails
                }

                // Schedule consolidated notification for item activation
                try
                {
                    await _consolidatedNotificationService.ScheduleItemActivatedNotificationAsync(
                        pendingItem.ConsignorId!.Value, organizationId, item.Id);
                    _logger.LogInformation("Scheduled consolidated notification for consignor {ConsignorId}, item {ItemId}",
                        pendingItem.ConsignorId, item.Id);
                }
                catch (Exception notificationEx)
                {
                    _logger.LogWarning(notificationEx, "Failed to schedule consolidated notification for consignor {ConsignorId}, item {ItemId}",
                        pendingItem.ConsignorId, item.Id);
                    // Don't fail the entire import if notification scheduling fails
                }
            }
            catch (Exception ex)
            {
                result.Failed.Add(new FailedPendingImportDto
                {
                    PendingImportId = pendingItem.Id,
                    Reason = ex.Message
                });
            }
        }

        await _context.SaveChangesAsync();
        return result;
    }

    public async Task<List<PendingImportItemDto>> CreateFromManifestAsync(Guid organizationId, CreatePendingImportsFromManifestRequest request)
    {
        _logger.LogInformation("🔍 [CreateFromManifest] Checking manifest {ManifestId} for organization {OrgId}", request.ManifestId, organizationId);

        // Get the dropoff request (manifest)
        var manifest = await _context.DropoffRequests
            .Include(dr => dr.Consignor)
            .FirstOrDefaultAsync(dr => dr.Id == request.ManifestId && dr.OrganizationId == organizationId);

        if (manifest == null)
        {
            _logger.LogWarning("❌ [CreateFromManifest] Manifest {ManifestId} not found", request.ManifestId);
            throw new ArgumentException("Manifest not found");
        }

        // Check if manifest has already been imported
        if (manifest.Status == DropoffRequestStatus.Imported || manifest.ImportedAt.HasValue)
        {
            _logger.LogWarning("⚠️ [CreateFromManifest] Manifest {ManifestId} already imported on {ImportedAt} with status {Status}",
                request.ManifestId, manifest.ImportedAt, manifest.Status);

            // Return existing pending imports for this manifest instead of creating duplicates
            var existingImports = await _context.PendingImportItems
                .Include(pii => pii.Consignor)
                .Where(pii => pii.OrganizationId == organizationId &&
                             pii.SourceReference == request.ManifestId.ToString() &&
                             pii.Source == ImportSource.Manifest)
                .ToListAsync();

            _logger.LogInformation("✅ [CreateFromManifest] Returning {Count} existing pending imports for already-processed manifest", existingImports.Count);
            return existingImports.Select(MapToPendingImportItemDto).ToList();
        }

        _logger.LogInformation("✅ [CreateFromManifest] Manifest {ManifestId} not yet imported, proceeding with creation", request.ManifestId);
        _logger.LogInformation("👤 [CreateFromManifest] Manifest belongs to ConsignorId: {ConsignorId}, AutoAssign: {AutoAssign}",
            manifest.ConsignorId, request.AutoAssignConsignor);

        // Parse items from JSON
        var manifestItems = System.Text.Json.JsonSerializer.Deserialize<List<ManifestItemDto>>(manifest.ItemsJson ?? "[]");

        if (manifestItems == null || !manifestItems.Any())
            return new List<PendingImportItemDto>();

        var assignedConsignorId = request.AutoAssignConsignor ? (Guid?)manifest.ConsignorId : null;
        _logger.LogInformation("📋 [CreateFromManifest] Creating {ItemCount} pending imports, each assigned to ConsignorId: {AssignedConsignorId}",
            manifestItems.Count, assignedConsignorId);

        var pendingImportRequests = manifestItems.Select(item => new CreatePendingImportItemRequest
        {
            Name = item.Name ?? "Unknown Item",
            Description = item.Notes,
            Price = item.SuggestedPrice ?? 0,
            MinimumPrice = item.MinimumPrice,
            Category = item.Category,
            Brand = item.Brand,
            Condition = item.SuggestedCondition,
            ImageUrl = item.ImageUrl, // Now should be normal Cloudinary URLs (~150 chars)
            Source = ImportSource.Manifest,
            SourceReference = request.ManifestId.ToString(),
            ConsignorId = assignedConsignorId,
            Notes = item.Notes
        }).ToList();

        var createdImports = await CreatePendingImportItemsBulkAsync(organizationId, pendingImportRequests);

        // Mark manifest as imported to prevent future duplicate processing
        manifest.Status = DropoffRequestStatus.Imported;
        manifest.ImportedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("✅ [CreateFromManifest] Manifest {ManifestId} marked as imported with {Count} pending items",
            request.ManifestId, createdImports.Count);

        return createdImports;
    }

    public async Task<List<PendingImportItemDto>> CreateFromCsvAsync(Guid organizationId, CreatePendingImportsFromCsvRequest request)
    {
        var pendingImportRequests = request.Items.Select(csvItem =>
        {
            Guid? consignorId = null;

            // Try to find consignor by number or email if provided
            if (!string.IsNullOrWhiteSpace(csvItem.ConsignorNumber))
            {
                var foundConsignorId = _context.Consignors
                    .Where(c => c.OrganizationId == organizationId &&
                               (c.ConsignorNumber == csvItem.ConsignorNumber || c.Email == csvItem.ConsignorNumber))
                    .Select(c => (Guid?)c.Id)
                    .FirstOrDefault();

                consignorId = foundConsignorId;
            }

            return new CreatePendingImportItemRequest
            {
                Name = csvItem.Name,
                Description = csvItem.Description,
                Price = csvItem.Price,
                Sku = csvItem.Sku,
                Category = csvItem.Category,
                Condition = csvItem.Condition,
                Source = ImportSource.CSV,
                SourceReference = request.FileName,
                ConsignorId = consignorId,
                Notes = csvItem.Notes
            };
        }).ToList();

        return await CreatePendingImportItemsBulkAsync(organizationId, pendingImportRequests);
    }

    private IQueryable<PendingImportItem> ApplySorting(IQueryable<PendingImportItem> query, string sortBy, string sortDirection)
    {
        var descending = sortDirection.ToLower() == "desc";

        return sortBy.ToLower() switch
        {
            "name" => descending ? query.OrderByDescending(pii => pii.Name) : query.OrderBy(pii => pii.Name),
            "price" => descending ? query.OrderByDescending(pii => pii.Price) : query.OrderBy(pii => pii.Price),
            "source" => descending ? query.OrderByDescending(pii => pii.Source) : query.OrderBy(pii => pii.Source),
            "status" => descending ? query.OrderByDescending(pii => pii.Status) : query.OrderBy(pii => pii.Status),
            "createdat" or _ => descending ? query.OrderByDescending(pii => pii.CreatedAt) : query.OrderBy(pii => pii.CreatedAt)
        };
    }

    private PendingImportItemDto MapToPendingImportItemDto(PendingImportItem entity)
    {
        return new PendingImportItemDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            Price = entity.Price,
            MinimumPrice = entity.MinimumPrice,
            Sku = entity.Sku,
            Category = entity.Category,
            Brand = entity.Brand,
            Condition = entity.Condition,
            ImageUrl = entity.ImageUrl,
            Source = entity.Source,
            SourceReference = entity.SourceReference,
            ConsignorId = entity.ConsignorId,
            ConsignorName = entity.Consignor?.FirstName != null && entity.Consignor?.LastName != null
                ? $"{entity.Consignor.FirstName} {entity.Consignor.LastName}".Trim()
                : null,
            ConsignorNumber = entity.Consignor?.ConsignorNumber,
            Status = entity.Status,
            ImportedAt = entity.ImportedAt,
            ImportedItemId = entity.ImportedItemId,
            CreatedAt = entity.CreatedAt,
            Notes = entity.Notes
        };
    }

    private ItemCondition ParseItemCondition(string? condition)
    {
        if (string.IsNullOrWhiteSpace(condition))
            return ItemCondition.Good;

        return condition.ToLower().Trim() switch
        {
            "new" => ItemCondition.New,
            "like new" or "likenew" => ItemCondition.LikeNew,
            "good" => ItemCondition.Good,
            "fair" => ItemCondition.Fair,
            "poor" => ItemCondition.Poor,
            _ => ItemCondition.Good
        };
    }

    public async Task<PendingImportItemDto> CreateFromSquareAsync(Guid organizationId, CreatePendingImportFromSquareRequest request)
    {
        try
        {
            // Check if this Square item already exists
            var existingItem = await _context.PendingImportItems
                .FirstOrDefaultAsync(p => p.OrganizationId == organizationId &&
                                         p.SourceReference == request.SquareCatalogId &&
                                         p.Source == ImportSource.Square);

            if (existingItem != null)
            {
                _logger.LogInformation("🔄 [PendingImportWrite] SQUARE UPDATE EXISTING - ID: {ExistingId}, Name: {Name}, CatalogId: {CatalogId}, OrgId: {OrgId}",
                    existingItem.Id, request.Name, request.SquareCatalogId, organizationId);

                // Update existing item with latest Square data
                existingItem.Name = request.Name;
                existingItem.Description = request.Description;
                existingItem.Price = request.Price;
                existingItem.Sku = request.Sku;
                existingItem.Category = request.Category;
                existingItem.Condition = request.Condition;
                existingItem.Notes = request.Notes;
                existingItem.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ [PendingImportWrite] SQUARE UPDATE COMPLETED - ID: {ExistingId}, Name: {Name}",
                    existingItem.Id, existingItem.Name);

                return MapToPendingImportItemDto(existingItem);
            }

            _logger.LogInformation("🔄 [PendingImportWrite] SQUARE ADD - Name: {Name}, CatalogId: {CatalogId}, VariationId: {VariationId}, OrgId: {OrgId}, Price: {Price}",
                request.Name, request.SquareCatalogId, request.SquareVariationId, organizationId, request.Price);

            // Create new pending import item for Square data
            var pendingItem = new PendingImportItem
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                Sku = request.Sku,
                Category = request.Category,
                Condition = request.Condition,
                Source = ImportSource.Square,
                SourceReference = request.SquareCatalogId,
                ConsignorId = null, // Will be assigned later
                Status = ImportStatus.Pending,
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow
            };

            _context.PendingImportItems.Add(pendingItem);
            await _context.SaveChangesAsync();

            _logger.LogInformation("✅ [PendingImportWrite] SQUARE ADD COMPLETED - ID: {EntityId}, Name: {Name}, CatalogId: {CatalogId}",
                pendingItem.Id, pendingItem.Name, request.SquareCatalogId);

            _logger.LogInformation("Created pending import item from Square: {Name} (Catalog ID: {CatalogId})",
                request.Name, request.SquareCatalogId);

            return MapToPendingImportItemDto(pendingItem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating pending import item from Square data: {Name}", request.Name);
            throw;
        }
    }

    /// <summary>
    /// Gets an existing category ID or creates a new category if it doesn't exist
    /// </summary>
    private async Task<Guid?> GetOrCreateCategoryIdAsync(string? categoryName, Guid organizationId)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
            return null;

        // First try to find existing category
        var existingCategory = await _context.ItemCategories
            .FirstOrDefaultAsync(c => c.Name == categoryName && c.OrganizationId == organizationId);

        if (existingCategory != null)
        {
            return existingCategory.Id;
        }

        // Create new category if it doesn't exist
        var newCategory = new ItemCategory
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = categoryName,
            Description = null,
            Color = null,
            IsActive = true,
            ParentCategoryId = null,
            SortOrder = 0,
            DefaultCommissionRate = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ItemCategories.Add(newCategory);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created new category: {CategoryName} for organization {OrganizationId}",
            categoryName, organizationId);

        return newCategory.Id;
    }

    // Helper DTO for manifest item parsing
    private class ManifestItemDto
    {
        public string? Name { get; set; }
        public string? Category { get; set; }
        public string? Brand { get; set; }
        public string? SuggestedCondition { get; set; }
        public decimal? SuggestedPrice { get; set; }
        public decimal? MinimumPrice { get; set; }
        public string? ImageUrl { get; set; }
        public string? ImagePublicId { get; set; }
        public string? Notes { get; set; }
    }
}