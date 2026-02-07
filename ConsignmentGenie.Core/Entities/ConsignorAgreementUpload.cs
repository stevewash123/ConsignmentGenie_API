using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConsignmentGenie.Core.Entities;

public class ConsignorAgreementUpload : BaseEntity
{
    [Required]
    public Guid ConsignorId { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(500)]
    public string FileUrl { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? FileMimeType { get; set; }

    public int? FileSizeBytes { get; set; }

    [Column(TypeName = "jsonb")]
    public string? VerificationResult { get; set; }

    [MaxLength(50)]
    public string? VerificationProvider { get; set; }

    [MaxLength(20)]
    public string? Recommendation { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public Guid? ReviewedBy { get; set; }

    [MaxLength(20)]
    public string? ReviewDecision { get; set; }

    [MaxLength(500)]
    public string? ReviewNote { get; set; }

    // Navigation properties
    public Consignor Consignor { get; set; } = null!;
    public User? ReviewedByUser { get; set; }
}