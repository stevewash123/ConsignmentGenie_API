using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.Entities;

public class ConsignorAgreement : BaseEntity
{
    [Required]
    public Guid ConsignorId { get; set; }

    [Required]
    public Guid OrganizationId { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "pending"; // 'pending', 'on_file', 'uploaded'

    [MaxLength(30)]
    public string? Method { get; set; } // 'consignor_upload', 'consignor_acknowledge', 'owner_marked', 'owner_upload'

    [MaxLength(500)]
    public string? DocumentUrl { get; set; }

    [MaxLength(200)]
    public string? DocumentStorageKey { get; set; }

    public string? Notes { get; set; }

    public DateTime? ReceivedAt { get; set; }

    public Guid? MarkedByUserId { get; set; }

    // Navigation properties
    public Consignor Consignor { get; set; } = null!;
    public Organization Organization { get; set; } = null!;
    public User? MarkedByUser { get; set; }
}