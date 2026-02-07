namespace ConsignmentGenie.Core.DTOs;

public class DropoffItemDto
{
    public string Name { get; set; } = "";
    public string? Category { get; set; }
    public string? Brand { get; set; }
    public string? Size { get; set; }
    public string? Color { get; set; }
    public string? SuggestedCondition { get; set; }
    public decimal SuggestedPrice { get; set; }
    public decimal? MinimumPrice { get; set; }
    public string? ImagePublicId { get; set; }
    public string? ImageUrl { get; set; }
    public string? Notes { get; set; }
}