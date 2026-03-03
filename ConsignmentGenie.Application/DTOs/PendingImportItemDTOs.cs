using ConsignmentGenie.Core.Entities;

namespace ConsignmentGenie.Application.DTOs;

/// <summary>
/// DTO for displaying pending import items
/// </summary>
public class PendingImportItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public decimal? MinimumPrice { get; set; }
    public string? Sku { get; set; }
    public string? Category { get; set; }
    public string? Brand { get; set; }
    public string? Condition { get; set; }
    public string? ImageUrl { get; set; }
    public ImportSource Source { get; set; }
    public string? SourceReference { get; set; }
    public Guid? ConsignorId { get; set; }
    public string? ConsignorName { get; set; }
    public string? ConsignorNumber { get; set; }
    public ImportStatus Status { get; set; }
    public DateTime? ImportedAt { get; set; }
    public Guid? ImportedItemId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Request to create a new pending import item
/// </summary>
public class CreatePendingImportItemRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public decimal? MinimumPrice { get; set; }
    public string? Sku { get; set; }
    public string? Category { get; set; }
    public string? Brand { get; set; }
    public string? Condition { get; set; }
    public string? ImageUrl { get; set; }
    public ImportSource Source { get; set; }
    public string? SourceReference { get; set; }
    public Guid? ConsignorId { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Request to update an existing pending import item
/// </summary>
public class UpdatePendingImportItemRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public string? Sku { get; set; }
    public string? Category { get; set; }
    public string? Condition { get; set; }
    public Guid? ConsignorId { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Request for partial update of pending import item (PATCH operation)
/// Used for inline editing and modal editing of pending import fields
/// </summary>
public class PatchPendingImportItemRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public string? Category { get; set; }
    public string? Condition { get; set; }
    public string? Brand { get; set; }
}

/// <summary>
/// Query parameters for pending import items
/// </summary>
public class PendingImportItemsQueryParams
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public string? Search { get; set; }
    public ImportSource? Source { get; set; }
    public ImportStatus? Status { get; set; }
    public Guid? ConsignorId { get; set; }
    public string? SourceReference { get; set; }
    public string? Category { get; set; }
    public string? Condition { get; set; }
    public string? SortBy { get; set; } = "CreatedAt";
    public string? SortDirection { get; set; } = "desc";
}

/// <summary>
/// Request to assign consignor to a pending import item
/// </summary>
public class AssignConsignorToPendingImportRequest
{
    public Guid ConsignorId { get; set; }
}

/// <summary>
/// Request for bulk assignment of multiple pending imports
/// </summary>
public class BulkAssignPendingImportsRequest
{
    public List<Guid> PendingImportIds { get; set; } = new List<Guid>();
    public Guid ConsignorId { get; set; }
    public bool MarkAsVerified { get; set; } = false;
}

/// <summary>
/// Response for bulk assignment operation
/// </summary>
public class BulkAssignPendingImportsResult
{
    public int Assigned { get; set; }
    public List<FailedPendingImportAssignmentDto> Failed { get; set; } = new List<FailedPendingImportAssignmentDto>();
    public List<PendingImportItemDto> UpdatedItems { get; set; } = new List<PendingImportItemDto>();
}

/// <summary>
/// Details of a failed assignment in bulk operation
/// </summary>
public class FailedPendingImportAssignmentDto
{
    public Guid PendingImportId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Request to import verified pending import items
/// </summary>
public class ImportVerifiedItemsRequest
{
    public List<Guid> PendingImportIds { get; set; } = new List<Guid>();
}

/// <summary>
/// Response for import operation
/// </summary>
public class ImportPendingItemsResult
{
    public int Imported { get; set; }
    public List<FailedPendingImportDto> Failed { get; set; } = new List<FailedPendingImportDto>();
    public List<Guid> ImportedItemIds { get; set; } = new List<Guid>();
}

/// <summary>
/// Details of a failed import in bulk operation
/// </summary>
public class FailedPendingImportDto
{
    public Guid PendingImportId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Request to create pending imports from manifest
/// </summary>
public class CreatePendingImportsFromManifestRequest
{
    public Guid ManifestId { get; set; }
    public bool AutoAssignConsignor { get; set; } = true;
}

/// <summary>
/// Request to create pending imports from CSV upload
/// </summary>
public class CreatePendingImportsFromCsvRequest
{
    public string FileName { get; set; } = string.Empty;
    public List<CsvImportItemDto> Items { get; set; } = new List<CsvImportItemDto>();
}

/// <summary>
/// CSV item data for import
/// </summary>
public class CsvImportItemDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? Sku { get; set; }
    public string? Category { get; set; }
    public string? Condition { get; set; }
    public string? ConsignorNumber { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Request to create pending import from Square data
/// </summary>
public class CreatePendingImportFromSquareRequest
{
    public string SquareCatalogId { get; set; } = string.Empty;
    public string SquareVariationId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? Sku { get; set; }
    public string? Category { get; set; }
    public string? Condition { get; set; }
    public DateTime SquareUpdatedAt { get; set; }
    public string? Notes { get; set; }
}