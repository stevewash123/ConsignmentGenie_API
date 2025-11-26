namespace ConsignmentGenie.Application.DTOs.Shopper;

public class StoreInfoDto
{
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public StoreHoursDto? Hours { get; set; }
    public bool IsOpen { get; set; }
}

public class StoreHoursDto
{
    public Dictionary<DayOfWeek, StoreHourDto> Hours { get; set; } = new();
}

public class StoreHourDto
{
    public bool IsOpen { get; set; }
    public TimeOnly? OpenTime { get; set; }
    public TimeOnly? CloseTime { get; set; }
}