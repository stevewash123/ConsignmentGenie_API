using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.Entities;

public class UserSession : BaseEntity
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(256)]
    public string SessionToken { get; set; } = string.Empty;

    public DateTime SessionStart { get; set; } = DateTime.UtcNow;

    public DateTime? SessionEnd { get; set; }

    [MaxLength(45)]
    public string? IPAddress { get; set; }

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime LastActivity { get; set; } = DateTime.UtcNow;

    public int ActionsCount { get; set; } = 0;

    // Navigation properties
    public User User { get; set; } = null!;

    // Computed properties
    public TimeSpan? SessionDuration => SessionEnd?.Subtract(SessionStart);
}