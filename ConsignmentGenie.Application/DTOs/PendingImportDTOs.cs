namespace ConsignmentGenie.Application.DTOs;

/// <summary>
/// DTO for displaying pending Square import items
/// </summary>
public class PendingSquareImportDto
{
    public Guid PendingImportId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? Sku { get; set; }
    public string? Category { get; set; }
    public DateTime ImportedAt { get; set; }
    public string SquareCatalogId { get; set; } = string.Empty;
    public string? SquareVariationId { get; set; }
    public DateTime? SquareUpdatedAt { get; set; }
    public string Status { get; set; } = "pending_assignment";
}

/// <summary>
/// Request to assign a consignor to a pending import
/// </summary>
public class AssignConsignorRequest
{
    public Guid ConsignorId { get; set; }
}

/// <summary>
/// Request for bulk assignment of multiple pending imports
/// </summary>
public class BulkAssignRequest
{
    public List<Guid> PendingImportIds { get; set; } = new List<Guid>();
    public Guid ConsignorId { get; set; }
}

/// <summary>
/// Response for bulk assignment operation
/// </summary>
public class BulkAssignResponse
{
    public int Assigned { get; set; }
    public List<FailedAssignmentDto> Failed { get; set; } = new List<FailedAssignmentDto>();
}

/// <summary>
/// Details of a failed assignment in bulk operation
/// </summary>
public class FailedAssignmentDto
{
    public Guid PendingImportId { get; set; }
    public string Reason { get; set; } = string.Empty;
}