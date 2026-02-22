using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ConsignmentGenie.Infrastructure.Data;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Application.Services.Interfaces;
using System.Security.Claims;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/owner/consignors/{consignorId}/agreement")]
[Authorize(Roles = "Owner")]
public class OwnerConsignorAgreementController : ControllerBase
{
    private readonly ConsignmentGenieContext _context;
    private readonly IPhotoService _photoService;
    private readonly ILogger<OwnerConsignorAgreementController> _logger;

    public OwnerConsignorAgreementController(
        ConsignmentGenieContext context,
        IPhotoService photoService,
        ILogger<OwnerConsignorAgreementController> logger)
    {
        _context = context;
        _photoService = photoService;
        _logger = logger;
    }

    /// <summary>
    /// Get agreement status for a specific consignor
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ConsignorAgreementStatusDto>> GetAgreementStatus(Guid consignorId)
    {
        try
        {
            var organizationId = GetOrganizationId();
            _logger.LogInformation("[OWNER_AGREEMENT] Getting agreement status for consignor {ConsignorId}", consignorId);

            // Verify consignor belongs to organization
            var consignor = await _context.Consignors
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == consignorId && c.OrganizationId == organizationId);

            if (consignor == null)
            {
                return NotFound("Consignor not found");
            }

            // Get agreement record if it exists
            var agreement = await _context.ConsignorAgreements
                .Include(ca => ca.MarkedByUser)
                .FirstOrDefaultAsync(ca => ca.ConsignorId == consignorId);

            // Determine status based on agreement record
            var status = agreement?.Status ?? "none";
            var method = agreement?.Method;
            var receivedAt = agreement?.ReceivedAt;
            var documentUrl = agreement?.DocumentUrl;
            var markedBy = agreement?.MarkedByUser != null ? new {
                id = agreement.MarkedByUser.Id.ToString(),
                name = $"{agreement.MarkedByUser.Name} {agreement.MarkedByUser}".Trim()
            } : null;

            return Ok(new ConsignorAgreementStatusDto
            {
                Status = status,
                Method = method,
                ReceivedAt = receivedAt,
                DocumentUrl = documentUrl,
                MarkedBy = markedBy
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[OWNER_AGREEMENT] Error getting agreement status for consignor {ConsignorId}", consignorId);
            return StatusCode(500, "An error occurred while retrieving agreement status");
        }
    }

    /// <summary>
    /// Mark agreement as on file (owner marked)
    /// </summary>
    [HttpPost("mark-on-file")]
    public async Task<ActionResult<MarkOnFileResponseDto>> MarkOnFile(Guid consignorId, [FromBody] MarkOnFileRequestDto request)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var userId = GetUserId();
            _logger.LogInformation("[OWNER_AGREEMENT] Marking agreement on file for consignor {ConsignorId}", consignorId);

            // Verify consignor belongs to organization
            var consignor = await _context.Consignors
                .FirstOrDefaultAsync(c => c.Id == consignorId && c.OrganizationId == organizationId);

            if (consignor == null)
            {
                return NotFound("Consignor not found");
            }

            // Create or update agreement record
            var agreement = await _context.ConsignorAgreements
                .FirstOrDefaultAsync(ca => ca.ConsignorId == consignorId);

            if (agreement == null)
            {
                agreement = new ConsignorAgreement
                {
                    ConsignorId = consignorId,
                    OrganizationId = organizationId
                };
                _context.ConsignorAgreements.Add(agreement);
            }

            agreement.Status = "on_file";
            agreement.Method = "owner_marked";
            agreement.ReceivedAt = DateTime.UtcNow;
            agreement.MarkedByUserId = userId;
            agreement.Notes = request.Notes;

            await _context.SaveChangesAsync();

            // Check for auto-approval
            await CheckAutoApproval(consignor, organizationId);

            _logger.LogInformation("[OWNER_AGREEMENT] Agreement marked on file for consignor {ConsignorId}", consignorId);

            return Ok(new MarkOnFileResponseDto
            {
                Status = "on_file",
                Method = "owner_marked",
                ReceivedAt = agreement.ReceivedAt.Value,
                Notes = agreement.Notes
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[OWNER_AGREEMENT] Error marking agreement on file for consignor {ConsignorId}", consignorId);
            return StatusCode(500, "An error occurred while marking agreement on file");
        }
    }

    /// <summary>
    /// Upload agreement document
    /// </summary>
    [HttpPost("upload")]
    public async Task<ActionResult<UploadAgreementResponseDto>> UploadAgreement(Guid consignorId, [FromForm] OwnerUploadAgreementRequestDto request)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var userId = GetUserId();
            _logger.LogInformation("[OWNER_AGREEMENT] Uploading agreement document for consignor {ConsignorId}", consignorId);

            // Verify consignor belongs to organization
            var consignor = await _context.Consignors
                .FirstOrDefaultAsync(c => c.Id == consignorId && c.OrganizationId == organizationId);

            if (consignor == null)
            {
                return NotFound("Consignor not found");
            }

            // Validate file
            if (request.File == null || request.File.Length == 0)
            {
                return BadRequest("File is required");
            }

            if (request.File.Length > 10 * 1024 * 1024) // 10MB limit
            {
                return BadRequest("File size cannot exceed 10MB");
            }

            var allowedTypes = new[] { "application/pdf", "image/png", "image/jpeg" };
            if (!allowedTypes.Contains(request.File.ContentType))
            {
                return BadRequest("Only PDF, PNG, and JPG files are allowed");
            }

            // Upload file
            using var fileStream = request.File.OpenReadStream();
            var fileUrl = await _photoService.UploadPhotoAsync(
                organizationId,
                consignorId,
                fileStream,
                request.File.FileName);

            if (string.IsNullOrEmpty(fileUrl))
            {
                return BadRequest("File upload failed");
            }

            // Create or update agreement record
            var agreement = await _context.ConsignorAgreements
                .FirstOrDefaultAsync(ca => ca.ConsignorId == consignorId);

            if (agreement == null)
            {
                agreement = new ConsignorAgreement
                {
                    ConsignorId = consignorId,
                    OrganizationId = organizationId
                };
                _context.ConsignorAgreements.Add(agreement);
            }

            agreement.Status = "uploaded";
            agreement.Method = "owner_upload";
            agreement.ReceivedAt = DateTime.UtcNow;
            agreement.MarkedByUserId = userId;
            agreement.DocumentUrl = fileUrl;
            agreement.Notes = request.Notes;

            await _context.SaveChangesAsync();

            // Check for auto-approval
            await CheckAutoApproval(consignor, organizationId);

            _logger.LogInformation("[OWNER_AGREEMENT] Agreement document uploaded for consignor {ConsignorId}", consignorId);

            return Ok(new UploadAgreementResponseDto
            {
                Status = "uploaded",
                Method = "owner_upload",
                ReceivedAt = agreement.ReceivedAt.Value,
                DocumentUrl = fileUrl,
                Notes = agreement.Notes
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[OWNER_AGREEMENT] Error uploading agreement for consignor {ConsignorId}", consignorId);
            return StatusCode(500, "An error occurred while uploading agreement");
        }
    }

    /// <summary>
    /// Remove agreement (resets to pending if required)
    /// </summary>
    [HttpDelete]
    public async Task<ActionResult> RemoveAgreement(Guid consignorId)
    {
        try
        {
            var organizationId = GetOrganizationId();
            _logger.LogInformation("[OWNER_AGREEMENT] Removing agreement for consignor {ConsignorId}", consignorId);

            // Verify consignor belongs to organization
            var consignor = await _context.Consignors
                .FirstOrDefaultAsync(c => c.Id == consignorId && c.OrganizationId == organizationId);

            if (consignor == null)
            {
                return NotFound("Consignor not found");
            }

            // Get agreement record
            var agreement = await _context.ConsignorAgreements
                .FirstOrDefaultAsync(ca => ca.ConsignorId == consignorId);

            if (agreement != null)
            {
                // Delete stored document if exists
                if (!string.IsNullOrEmpty(agreement.DocumentUrl))
                {
                    try
                    {
                        // Note: Would need to implement deletion in photo service
                        // await _photoService.DeletePhotoAsync(agreement.DocumentUrl);
                    }
                    catch (Exception deleteEx)
                    {
                        _logger.LogWarning(deleteEx, "[OWNER_AGREEMENT] Failed to delete document file for consignor {ConsignorId}", consignorId);
                    }
                }

                _context.ConsignorAgreements.Remove(agreement);
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("[OWNER_AGREEMENT] Agreement removed for consignor {ConsignorId}", consignorId);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[OWNER_AGREEMENT] Error removing agreement for consignor {ConsignorId}", consignorId);
            return StatusCode(500, "An error occurred while removing agreement");
        }
    }

    private async Task CheckAutoApproval(Consignor consignor, Guid organizationId)
    {
        // Get organization settings
        var organization = await _context.Organizations
            .FirstAsync(o => o.Id == organizationId);

        // Only auto-approve if organization has auto-approval enabled
        // AND consignor is currently in pending_agreement status
        if (organization.ApprovalMode == ApprovalMode.Auto && consignor.Status == Core.Enums.ConsignorStatus.PendingAgreement)
        {
            consignor.Status = Core.Enums.ConsignorStatus.Active;
            _logger.LogInformation("[OWNER_AGREEMENT] Auto-approved consignor {ConsignorId} after agreement completion", consignor.Id);

            // Could add notification here if needed
            // await _notificationService.CreateNotification(organizationId, $"Consignor {consignor.Name} {consignor} has been auto-approved");
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

// DTOs
public class ConsignorAgreementStatusDto
{
    public string Status { get; set; } = string.Empty; // "none" | "pending" | "on_file" | "uploaded"
    public string? Method { get; set; } // "consignor_upload" | "consignor_acknowledge" | "owner_marked" | "owner_upload" | null
    public DateTime? ReceivedAt { get; set; }
    public string? DocumentUrl { get; set; }
    public object? MarkedBy { get; set; } // { id: "uuid", name: "John Owner" } | null
}

public class MarkOnFileRequestDto
{
    public string? Notes { get; set; }
}

public class MarkOnFileResponseDto
{
    public string Status { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; }
    public string? Notes { get; set; }
}

public class OwnerUploadAgreementRequestDto
{
    public IFormFile File { get; set; } = null!;
    public string? Notes { get; set; }
}

public class UploadAgreementResponseDto
{
    public string Status { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; }
    public string DocumentUrl { get; set; } = string.Empty;
    public string? Notes { get; set; }
}