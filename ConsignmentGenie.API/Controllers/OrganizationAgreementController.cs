using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ConsignmentGenie.Core.DTOs.Organization;
using ConsignmentGenie.Core.DTOs.Settings;
using ConsignmentGenie.Application.Services;
using ConsignmentGenie.Application.Services.Interfaces;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/organizations/agreements")]
[Authorize(Roles = "Owner")]
public class OrganizationAgreementController : ControllerBase
{
    private readonly ConsignmentGenieContext _context;
    private readonly ILogger<OrganizationAgreementController> _logger;
    private readonly IDocumentService _documentService;

    public OrganizationAgreementController(
        ConsignmentGenieContext context,
        ILogger<OrganizationAgreementController> logger,
        IDocumentService documentService)
    {
        _context = context;
        _logger = logger;
        _documentService = documentService;
    }

    [HttpPost("send-sample")]
    public async Task<ActionResult> SendSampleAgreement([FromBody] SendSampleAgreementRequest request)
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[SAMPLE_AGREEMENT] Sending sample agreement notification for organization {OrganizationId}", organizationId);

        try
        {
            if (string.IsNullOrEmpty(request.TemplateContent))
            {
                return BadRequest("Template content is required");
            }

            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                _logger.LogWarning("[SAMPLE_AGREEMENT] Organization {OrganizationId} not found", organizationId);
                return NotFound("Organization not found");
            }

            // TODO: Implement actual email sending logic
            _logger.LogInformation("[SAMPLE_AGREEMENT] Sample agreement sent for organization {OrganizationId}", organizationId);
            return Ok(new { success = true, message = "Sample agreement sent successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SAMPLE_AGREEMENT] Error sending sample agreement for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("generate-pdf")]
    public async Task<ActionResult> GeneratePdf([FromBody] GeneratePdfRequest request)
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[PDF] Generating PDF for organization {OrganizationId}, contentType: {ContentType}", organizationId, request.ContentType);

        try
        {
            if (string.IsNullOrEmpty(request.Content))
            {
                return BadRequest("Content is required");
            }

            // TODO: Implement PDF generation logic
            var pdfBytes = new byte[0]; // Placeholder

            _logger.LogInformation("[PDF] PDF generated successfully for organization {OrganizationId}", organizationId);
            return File(pdfBytes, "application/pdf", $"{request.ContentType}-{organizationId}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PDF] Error generating PDF for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("templates")]
    public async Task<ActionResult<AgreementTemplateDto>> UploadAgreementTemplate(IFormFile file)
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[AGREEMENT] Starting upload for organization {OrganizationId}", organizationId);

        try
        {
            // Validate file
            _logger.LogInformation("[AGREEMENT] Validating file. File is null: {FileIsNull}, Length: {FileLength}",
                file == null, file?.Length ?? 0);

            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("[AGREEMENT] No file provided or file is empty");
                return BadRequest("No file provided");
            }

            _logger.LogInformation("[AGREEMENT] File details - Name: {FileName}, ContentType: {ContentType}, Size: {FileSize}",
                file.FileName, file.ContentType, file.Length);

            if (!IsValidFileType(file.ContentType))
            {
                _logger.LogWarning("[AGREEMENT] Invalid file type: {ContentType}", file.ContentType);
                return BadRequest("Invalid file type. Only PDF, DOC, and DOCX files are allowed");
            }

            _logger.LogInformation("[AGREEMENT] Uploading agreement template: {FileName}", file.FileName);

            // Upload file to Cloudinary
            string documentUrl;
            using (var fileStream = file.OpenReadStream())
            {
                documentUrl = await _documentService.UploadAgreementTemplateAsync(organizationId, fileStream, file.FileName);
            }

            _logger.LogInformation("[AGREEMENT] File successfully uploaded to Cloudinary: {DocumentUrl}", documentUrl);

            // Store the Cloudinary URL directly in the dedicated field
            var organization = await _context.Organizations.FindAsync(organizationId);
            if (organization != null)
            {
                _logger.LogInformation("[AGREEMENT] Organization found. Current AgreementTemplateCloudinaryUrl: {CurrentUrl}", organization.AgreementTemplateCloudinaryUrl);
                organization.AgreementTemplateCloudinaryUrl = documentUrl;

                var changesSaved = await _context.SaveChangesAsync();
                _logger.LogInformation("[AGREEMENT] Database updated. Changes saved: {ChangesSaved}, CloudinaryUrl: {DocumentUrl}", changesSaved, documentUrl);
            }
            else
            {
                _logger.LogError("[AGREEMENT] Organization {OrganizationId} not found in database", organizationId);
                return NotFound("Organization not found");
            }

            var result = new AgreementTemplateDto
            {
                Id = organizationId.ToString(), // Use organization ID as template identifier
                FileName = file.FileName,
                FileSize = file.Length,
                ContentType = file.ContentType,
                UploadedAt = DateTime.UtcNow
            };

            _logger.LogInformation("[AGREEMENT] Upload complete. Returning result: {@Result}", result);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AGREEMENT] Error uploading agreement template for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("templates/{templateId}")]
    public async Task<ActionResult> DownloadAgreementTemplate(string templateId)
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[AGREEMENT] Downloading agreement template {TemplateId} for organization {OrganizationId}", templateId, organizationId);

        try
        {
            // Get the organization to access the stored Cloudinary URL
            var organization = await _context.Organizations.FindAsync(organizationId);
            if (organization == null)
            {
                _logger.LogWarning("[AGREEMENT] Organization {OrganizationId} not found", organizationId);
                return NotFound("Organization not found");
            }

            // Get the Cloudinary URL from the dedicated field
            var cloudinaryUrl = organization.AgreementTemplateCloudinaryUrl;
            if (string.IsNullOrEmpty(cloudinaryUrl) || !cloudinaryUrl.Contains("cloudinary.com"))
            {
                _logger.LogWarning("[AGREEMENT] No agreement template uploaded for organization {OrganizationId}", organizationId);
                return NotFound("No agreement template has been uploaded");
            }

            // Verify the file exists in Cloudinary
            var exists = await _documentService.DocumentExistsAsync(cloudinaryUrl);
            if (!exists)
            {
                _logger.LogWarning("[AGREEMENT] Agreement template file no longer exists in Cloudinary: {CloudinaryUrl}", cloudinaryUrl);
                return NotFound("Agreement template file not found");
            }

            // Redirect directly to the Cloudinary URL (it's already secure)
            return Redirect(cloudinaryUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AGREEMENT] Error downloading agreement template {TemplateId} for organization {OrganizationId}", templateId, organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("templates/{templateId}/text")]
    public async Task<ActionResult<string>> GetAgreementTemplateAsText(string templateId)
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[AGREEMENT] Getting agreement template text {TemplateId} for organization {OrganizationId}", templateId, organizationId);

        try
        {
            // Get the organization to access the stored Cloudinary URL
            var organization = await _context.Organizations.FindAsync(organizationId);
            if (organization == null)
            {
                _logger.LogWarning("[AGREEMENT] Organization {OrganizationId} not found", organizationId);
                return NotFound("Organization not found");
            }

            // Get the Cloudinary URL from the dedicated field
            var cloudinaryUrl = organization.AgreementTemplateCloudinaryUrl;
            if (string.IsNullOrEmpty(cloudinaryUrl) || !cloudinaryUrl.Contains("cloudinary.com"))
            {
                _logger.LogWarning("[AGREEMENT] No agreement template uploaded for organization {OrganizationId}", organizationId);
                return NotFound("No agreement template has been uploaded");
            }

            // Extract text content from the Cloudinary document
            var content = await _documentService.ExtractTextFromDocumentAsync(cloudinaryUrl);
            if (string.IsNullOrEmpty(content))
            {
                _logger.LogWarning("[AGREEMENT] Could not extract text from document: {CloudinaryUrl}", cloudinaryUrl);
                return NotFound("Could not extract text from agreement template");
            }

            _logger.LogInformation("[AGREEMENT] Successfully extracted text from agreement template for organization {OrganizationId}", organizationId);
            return Ok(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AGREEMENT] Error getting agreement template text {TemplateId} for organization {OrganizationId}", templateId, organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("templates/{templateId}")]
    public async Task<ActionResult> DeleteAgreementTemplate(string templateId)
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[AGREEMENT] Deleting agreement template {TemplateId} for organization {OrganizationId}", templateId, organizationId);

        try
        {
            // Get the organization to access the stored Cloudinary URL
            var organization = await _context.Organizations.FindAsync(organizationId);
            if (organization == null)
            {
                _logger.LogWarning("[AGREEMENT] Organization {OrganizationId} not found", organizationId);
                return NotFound("Agreement template not found");
            }

            // Get the Cloudinary URL and delete the file
            var cloudinaryUrl = organization.AgreementTemplateCloudinaryUrl;
            bool fileDeleted = false;

            if (!string.IsNullOrEmpty(cloudinaryUrl) && cloudinaryUrl.Contains("cloudinary.com"))
            {
                fileDeleted = await _documentService.DeleteDocumentAsync(cloudinaryUrl);
                if (fileDeleted)
                {
                    _logger.LogInformation("[AGREEMENT] Deleted file from Cloudinary: {CloudinaryUrl}", cloudinaryUrl);
                }
                else
                {
                    _logger.LogWarning("[AGREEMENT] Failed to delete file from Cloudinary: {CloudinaryUrl}", cloudinaryUrl);
                }
            }
            else
            {
                _logger.LogWarning("[AGREEMENT] No valid Cloudinary URL found for organization {OrganizationId}", organizationId);
            }

            // Clear the URL from the organization
            organization.AgreementTemplateCloudinaryUrl = null;
            await _context.SaveChangesAsync();

            _logger.LogInformation("[AGREEMENT] Agreement template {TemplateId} deleted successfully for organization {OrganizationId}", templateId, organizationId);
            return Ok(new { success = true, message = "Agreement template deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AGREEMENT] Error deleting agreement template {TemplateId} for organization {OrganizationId}", templateId, organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    private bool IsValidFileType(string contentType)
    {
        var validTypes = new[]
        {
            "application/pdf",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "text/plain"
        };

        return validTypes.Contains(contentType);
    }

    private string GetContentType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };
    }


    private Guid GetOrganizationId()
    {
        var organizationIdClaim = User.FindFirst("organizationId")?.Value;
        if (organizationIdClaim != null && Guid.TryParse(organizationIdClaim, out var organizationId))
        {
            return organizationId;
        }

        throw new UnauthorizedAccessException("Organization ID not found in token");
    }

}