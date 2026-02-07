using ConsignmentGenie.Core.Enums;

namespace ConsignmentGenie.Application.DTOs;

public class SuggestionDto
{
    public Guid Id { get; set; }
    public SuggestionType Type { get; set; }
    public string Message { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public bool IsProcessed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}