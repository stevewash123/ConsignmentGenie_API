namespace ConsignmentGenie.Application.DTOs;

public class LookupDto
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int? SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}