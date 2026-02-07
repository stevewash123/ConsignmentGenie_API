namespace ConsignmentGenie.Core.DTOs.Items;

public class BulkImportRequest
{
    public List<CreateItemRequest> Items { get; set; } = new();

    // File tracking properties (optional)
    public string? FileName { get; set; }
    public string? FirstDataRow { get; set; } // For hash generation and duplicate detection

    // Source tracking properties
    public string? SourceType { get; set; } // "dropoff_manifest", "csv_upload", etc.
    public Guid? SourceReferenceId { get; set; } // ID of the dropoff request, etc.
}