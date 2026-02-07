using ConsignmentGenie.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.Entities;

public class UserPermission : BaseEntity
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Permission Permission { get; set; }

    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public Guid GrantedByUserId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? RevokedAt { get; set; }

    public Guid? RevokedByUserId { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public User GrantedByUser { get; set; } = null!;
    public User? RevokedByUser { get; set; }
}