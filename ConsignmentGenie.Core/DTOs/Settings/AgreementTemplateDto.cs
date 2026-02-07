namespace ConsignmentGenie.Core.DTOs.Settings;

public class AgreementTemplateDto
{
    public string Id { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; }
    public string ContentType { get; set; } = string.Empty;
}