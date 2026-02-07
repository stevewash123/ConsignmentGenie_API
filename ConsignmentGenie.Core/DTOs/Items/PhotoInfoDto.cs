namespace ConsignmentGenie.Core.DTOs.Items;

public class PhotoInfoDto
{
    public string Url { get; set; } = string.Empty;
    public string PublicId { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public int Order { get; set; }
    public string UploadedAt { get; set; } = string.Empty;
}