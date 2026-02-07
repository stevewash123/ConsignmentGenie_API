using System.Text.Json.Serialization;

namespace ConsignmentGenie.Core.DTOs.Items;

public class PhotoInfoDto
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("publicId")]
    public string PublicId { get; set; } = string.Empty;

    [JsonPropertyName("isPrimary")]
    public bool IsPrimary { get; set; }

    [JsonPropertyName("order")]
    public int Order { get; set; }

    [JsonPropertyName("uploadedAt")]
    public string UploadedAt { get; set; } = string.Empty;
}