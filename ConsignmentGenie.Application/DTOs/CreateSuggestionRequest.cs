using System.ComponentModel.DataAnnotations;
using ConsignmentGenie.Core.Enums;

namespace ConsignmentGenie.Application.DTOs;

public class CreateSuggestionRequest
{
    [Required]
    public SuggestionType Type { get; set; }

    [Required]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "Suggestion must be between 10 and 2000 characters")]
    public string Message { get; set; } = string.Empty;
}