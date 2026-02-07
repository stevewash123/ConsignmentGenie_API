using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.Entities;

public class FileUploadHash : BaseEntity
{
    [Required]
    public Guid OrganizationId { get; set; }

    [Required]
    [StringLength(64)] // SHA-256 hash length - includes OrganizationId + first data row
    public string Hash { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    public string FileType { get; set; } = string.Empty; // e.g., "bulk_import", "item_upload"

    public string? FirstRowSample { get; set; } // Store sample of first data row for verification

    public int RowCount { get; set; } // Number of rows in the file

    // Navigation property
    public Organization Organization { get; set; } = null!;
}