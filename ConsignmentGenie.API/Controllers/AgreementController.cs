using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ConsignmentGenie.Infrastructure.Data;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Application.Services.Interfaces;
using System.Security.Claims;
using System.Text.Json;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/agreements")]
[Authorize]
public class AgreementController : ControllerBase
{
    private readonly ConsignmentGenieContext _context;
    private readonly IAgreementVerificationService _verificationService;
    private readonly IPhotoService _photoService;
    private readonly ILogger<AgreementController> _logger;

    public AgreementController(
        ConsignmentGenieContext context,
        IAgreementVerificationService verificationService,
        IPhotoService photoService,
        ILogger<AgreementController> logger)
    {
        _context = context;
        _verificationService = verificationService;
        _photoService = photoService;
        _logger = logger;
    }

    // POST api/agreements/upload
    [HttpPost("upload")]
    [Authorize(Roles = "Owner,Consignor")]
    public async Task<ActionResult<ConsignorAgreementUploadDto>> UploadAgreement([FromForm] UploadAgreementRequest request)
    {
        _logger.LogInformation("[AGREEMENT] Starting agreement upload for consignor {ConsignorId}", request.ConsignorId);

        try
        {
            var organizationId = GetOrganizationId();
            var userId = GetUserId();

            // Validate consignor exists and belongs to organization
            var consignor = await _context.Consignors.FirstOrDefaultAsync(c =>
                c.Id == request.ConsignorId && c.OrganizationId == organizationId);

            if (consignor == null)
            {
                _logger.LogWarning("[AGREEMENT] Consignor {ConsignorId} not found in organization {OrganizationId}",
                    request.ConsignorId, organizationId);
                return NotFound("Consignor not found");
            }

            // If user is a consignor, they can only upload their own agreements
            if (User.IsInRole("Consignor"))
            {
                var userConsignor = await _context.Consignors.FirstOrDefaultAsync(c => c.UserId == userId);
                if (userConsignor?.Id != request.ConsignorId)
                {
                    _logger.LogWarning("[AGREEMENT] Consignor user {UserId} attempted to upload agreement for different consignor {ConsignorId}",
                        userId, request.ConsignorId);
                    return Forbid("You can only upload agreements for yourself");
                }
            }

            // Validate uploaded file
            if (request.AgreementFile == null || request.AgreementFile.Length == 0)
            {
                return BadRequest("Agreement file is required");
            }

            // Upload file using photo service
            using var fileStream = request.AgreementFile.OpenReadStream();
            var fileUrl = await _photoService.UploadPhotoAsync(
                organizationId,
                request.ConsignorId, // Using consignor ID as a unique identifier for agreement files
                fileStream,
                request.AgreementFile.FileName);

            if (string.IsNullOrEmpty(fileUrl))
            {
                _logger.LogError("[AGREEMENT] File upload failed for consignor {ConsignorId}", request.ConsignorId);
                return BadRequest("File upload failed");
            }

            // Validate document format and accessibility
            var validationResult = await _verificationService.ValidateDocumentAsync(
                fileUrl,
                request.AgreementFile.ContentType,
                (int)request.AgreementFile.Length);

            if (!validationResult.IsValid)
            {
                _logger.LogWarning("[AGREEMENT] Document validation failed for consignor {ConsignorId}: {Errors}",
                    request.ConsignorId, string.Join("; ", validationResult.Errors));
                return BadRequest(new { errors = validationResult.Errors, warnings = validationResult.Warnings });
            }

            // Create agreement upload record
            var upload = new ConsignorAgreementUpload
            {
                ConsignorId = request.ConsignorId,
                UploadedAt = DateTime.UtcNow,
                FileUrl = fileUrl,
                FileMimeType = request.AgreementFile.ContentType,
                FileSizeBytes = (int)request.AgreementFile.Length
            };

            _context.ConsignorAgreementUploads.Add(upload);

            // Start AI verification process
            try
            {
                var verificationResult = await _verificationService.VerifyAgreementAsync(
                    fileUrl, request.ConsignorId, organizationId);

                upload.VerificationResult = JsonSerializer.Serialize(verificationResult);
                upload.VerificationProvider = verificationResult.Provider;
                upload.Recommendation = verificationResult.Recommendation;

                _logger.LogInformation("[AGREEMENT] AI verification completed for consignor {ConsignorId}: {Recommendation}",
                    request.ConsignorId, verificationResult.Recommendation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AGREEMENT] AI verification failed for consignor {ConsignorId}, marking for manual review",
                    request.ConsignorId);
                upload.Recommendation = "manual_review";
            }

            // Update consignor agreement status based on recommendation
            if (upload.Recommendation == "approve")
            {
                consignor.AgreementStatus = "approved";
                consignor.SignedAgreementUploadedAt = DateTime.UtcNow;
                consignor.SignedAgreementUrl = fileUrl;
                _logger.LogInformation("[AGREEMENT] Auto-approved agreement for consignor {ConsignorId}", request.ConsignorId);
            }
            else
            {
                consignor.AgreementStatus = "pending_review";
                _logger.LogInformation("[AGREEMENT] Agreement marked for review for consignor {ConsignorId}", request.ConsignorId);
            }

            await _context.SaveChangesAsync();

            return Ok(new ConsignorAgreementUploadDto
            {
                Id = upload.Id,
                ConsignorId = upload.ConsignorId,
                UploadedAt = upload.UploadedAt,
                FileUrl = upload.FileUrl,
                Recommendation = upload.Recommendation,
                Status = consignor.AgreementStatus
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AGREEMENT] Error during agreement upload for consignor {ConsignorId}", request.ConsignorId);
            return StatusCode(500, "An error occurred while processing the agreement upload");
        }
    }

    // GET api/agreements/status/{consignorId}
    [HttpGet("status/{consignorId}")]
    [Authorize(Roles = "Owner,Consignor")]
    public async Task<ActionResult<AgreementStatusDto>> GetAgreementStatus(Guid consignorId)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var userId = GetUserId();

            var consignor = await _context.Consignors.FirstOrDefaultAsync(c =>
                c.Id == consignorId && c.OrganizationId == organizationId);

            if (consignor == null)
            {
                return NotFound("Consignor not found");
            }

            // If user is a consignor, they can only view their own status
            if (User.IsInRole("Consignor"))
            {
                var userConsignor = await _context.Consignors.FirstOrDefaultAsync(c => c.UserId == userId);
                if (userConsignor?.Id != consignorId)
                {
                    return Forbid("You can only view your own agreement status");
                }
            }

            // Get organization agreement settings
            var organization = await _context.Organizations.FirstAsync(o => o.Id == organizationId);

            // Get latest upload if any
            var latestUpload = await _context.ConsignorAgreementUploads
                .Where(u => u.ConsignorId == consignorId)
                .OrderByDescending(u => u.UploadedAt)
                .FirstOrDefaultAsync();

            return Ok(new AgreementStatusDto
            {
                ConsignorId = consignorId,
                AgreementMethod = organization.AgreementMethod,
                Status = consignor.AgreementStatus ?? "not_required",
                RequiredForItemSubmission = organization.AgreementMethod != "none",
                LatestUpload = latestUpload != null ? new ConsignorAgreementUploadDto
                {
                    Id = latestUpload.Id,
                    ConsignorId = latestUpload.ConsignorId,
                    UploadedAt = latestUpload.UploadedAt,
                    FileUrl = latestUpload.FileUrl,
                    Recommendation = latestUpload.Recommendation,
                    Status = consignor.AgreementStatus
                } : null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AGREEMENT] Error getting agreement status for consignor {ConsignorId}", consignorId);
            return StatusCode(500, "An error occurred while retrieving agreement status");
        }
    }

    // GET api/agreements/uploads/{consignorId}
    [HttpGet("uploads/{consignorId}")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<List<ConsignorAgreementUploadDto>>> GetAgreementUploads(Guid consignorId)
    {
        try
        {
            var organizationId = GetOrganizationId();

            var uploads = await _context.ConsignorAgreementUploads
                .Include(u => u.Consignor)
                .Where(u => u.ConsignorId == consignorId && u.Consignor.OrganizationId == organizationId)
                .OrderByDescending(u => u.UploadedAt)
                .ToListAsync();

            var uploadDtos = uploads.Select(u => new ConsignorAgreementUploadDto
            {
                Id = u.Id,
                ConsignorId = u.ConsignorId,
                UploadedAt = u.UploadedAt,
                FileUrl = u.FileUrl,
                Recommendation = u.Recommendation,
                ReviewedAt = u.ReviewedAt,
                ReviewDecision = u.ReviewDecision,
                ReviewNote = u.ReviewNote
            }).ToList();

            return Ok(uploadDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AGREEMENT] Error getting agreement uploads for consignor {ConsignorId}", consignorId);
            return StatusCode(500, "An error occurred while retrieving agreement uploads");
        }
    }

    // GET api/agreements/download/{consignorId}
    [HttpGet("download/{consignorId}")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult> DownloadSignedAgreement(Guid consignorId)
    {
        try
        {
            var organizationId = GetOrganizationId();

            var consignor = await _context.Consignors.FirstOrDefaultAsync(c =>
                c.Id == consignorId && c.OrganizationId == organizationId);

            if (consignor == null)
            {
                return NotFound("Consignor not found");
            }

            if (string.IsNullOrEmpty(consignor.SignedAgreementUrl))
            {
                return NotFound("No signed agreement on file for this consignor");
            }

            // Return redirect to the stored file URL (assumes file is publicly accessible)
            // Alternative: proxy the file through this endpoint if needed
            return Redirect(consignor.SignedAgreementUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AGREEMENT] Error downloading signed agreement for consignor {ConsignorId}", consignorId);
            return StatusCode(500, "An error occurred while downloading the agreement");
        }
    }

    // POST api/agreements/review/{uploadId}
    [HttpPost("review/{uploadId}")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult> ReviewAgreement(Guid uploadId, [FromBody] ReviewAgreementRequest request)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var userId = GetUserId();

            var upload = await _context.ConsignorAgreementUploads
                .Include(u => u.Consignor)
                .FirstOrDefaultAsync(u => u.Id == uploadId && u.Consignor.OrganizationId == organizationId);

            if (upload == null)
            {
                return NotFound("Agreement upload not found");
            }

            // Update upload record with review decision
            upload.ReviewedAt = DateTime.UtcNow;
            upload.ReviewedBy = userId;
            upload.ReviewDecision = request.Decision;
            upload.ReviewNote = request.Note;

            // Update consignor agreement status based on review decision
            var consignor = upload.Consignor;
            if (request.Decision == "approved")
            {
                consignor.AgreementStatus = "approved";
                consignor.SignedAgreementUploadedAt = upload.UploadedAt;
                consignor.SignedAgreementUrl = upload.FileUrl;
                consignor.AgreementReviewedAt = DateTime.UtcNow;
                consignor.AgreementReviewedBy = userId;
                consignor.AgreementReviewNote = request.Note;
            }
            else if (request.Decision == "rejected")
            {
                consignor.AgreementStatus = "rejected";
                consignor.AgreementReviewedAt = DateTime.UtcNow;
                consignor.AgreementReviewedBy = userId;
                consignor.AgreementReviewNote = request.Note;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("[AGREEMENT] Agreement {UploadId} reviewed by {UserId}: {Decision}",
                uploadId, userId, request.Decision);

            return Ok(new { message = "Agreement review completed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AGREEMENT] Error reviewing agreement {UploadId}", uploadId);
            return StatusCode(500, "An error occurred while processing the review");
        }
    }

    private Guid GetOrganizationId()
    {
        var organizationIdClaim = User.FindFirst("organizationId")?.Value;
        if (string.IsNullOrEmpty(organizationIdClaim) || !Guid.TryParse(organizationIdClaim, out var organizationId))
        {
            throw new UnauthorizedAccessException("Organization ID not found in token");
        }
        return organizationId;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        return userId;
    }
}

// DTOs for the API
public class UploadAgreementRequest
{
    public Guid ConsignorId { get; set; }
    public IFormFile AgreementFile { get; set; } = null!;
}

public class ReviewAgreementRequest
{
    public string Decision { get; set; } = string.Empty; // "approved" or "rejected"
    public string? Note { get; set; }
}

public class AgreementStatusDto
{
    public Guid ConsignorId { get; set; }
    public string AgreementMethod { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool RequiredForItemSubmission { get; set; }
    public ConsignorAgreementUploadDto? LatestUpload { get; set; }
}

public class ConsignorAgreementUploadDto
{
    public Guid Id { get; set; }
    public Guid ConsignorId { get; set; }
    public DateTime UploadedAt { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public string? Recommendation { get; set; }
    public string? Status { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewDecision { get; set; }
    public string? ReviewNote { get; set; }
}