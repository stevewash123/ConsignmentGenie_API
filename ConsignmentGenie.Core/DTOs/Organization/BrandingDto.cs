using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.DTOs.Organization;

public class BrandingDto
{
    public BrandingLogoDto Logo { get; set; } = new();
    public BrandingColorsDto Colors { get; set; } = new();
    public BrandingTypographyDto Typography { get; set; } = new();
    public BrandingStyleDto Style { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

public class BrandingLogoDto
{
    [MaxLength(500)]
    public string? Url { get; set; }

    [MaxLength(100)]
    public string? FileName { get; set; }

    public DateTime? UploadedAt { get; set; }

    public LogoDimensionsDto Dimensions { get; set; } = new();
}

public class LogoDimensionsDto
{
    public int Width { get; set; }
    public int Height { get; set; }
}

public class BrandingColorsDto
{
    [MaxLength(7)] // Hex color code
    public string Primary { get; set; } = "#3B82F6"; // Default blue

    [MaxLength(7)]
    public string Secondary { get; set; } = "#6B7280"; // Default gray

    [MaxLength(7)]
    public string Accent { get; set; } = "#10B981"; // Default green

    [MaxLength(7)]
    public string Text { get; set; } = "#1F2937"; // Default dark gray

    [MaxLength(7)]
    public string Background { get; set; } = "#FFFFFF"; // Default white
}

public class BrandingTypographyDto
{
    [MaxLength(50)]
    public string HeadingFont { get; set; } = "Inter";

    [MaxLength(50)]
    public string BodyFont { get; set; } = "Inter";

    [MaxLength(10)]
    public string FontSizeScale { get; set; } = "medium"; // small, medium, large
}

public class BrandingStyleDto
{
    [MaxLength(20)]
    public string Theme { get; set; } = "professional"; // professional, modern, vintage, minimal

    public string? CustomCss { get; set; }
}

/// <summary>
/// Request DTO for updating branding settings (partial updates)
/// </summary>
public class UpdateBrandingRequest
{
    public BrandingLogoDto? Logo { get; set; }
    public BrandingColorsDto? Colors { get; set; }
    public BrandingTypographyDto? Typography { get; set; }
    public BrandingStyleDto? Style { get; set; }
}

/// <summary>
/// Response DTO for logo upload
/// </summary>
public class LogoUploadResponse
{
    [Required]
    public string Url { get; set; } = string.Empty;

    public LogoDimensionsDto Dimensions { get; set; } = new();
}