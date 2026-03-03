using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ConsignmentGenie.Core.DTOs;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Interfaces;
using ConsignmentGenie.Application.Services;
using ConsignmentGenie.Application.DTOs;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/owner")]
[Authorize(Roles = "Owner")]
public class OwnerController : ControllerBase
{
    private readonly ILogger<OwnerController> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPendingImportItemService _pendingImportItemService;

    public OwnerController(ILogger<OwnerController> logger, IUnitOfWork unitOfWork, IPendingImportItemService pendingImportItemService)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
        _pendingImportItemService = pendingImportItemService;
    }

    // DROPOFF REQUESTS MANAGEMENT
    [HttpGet("dropoff-requests")]
    public async Task<ActionResult<List<OwnerDropoffRequestListDto>>> GetDropoffRequests()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return BadRequest("User not found");

            var user = await _unitOfWork.Users
                .GetAsync(u => u.Id == userId.Value, includeProperties: "Organization");

            if (user == null)
                return NotFound("User not found");

            var requests = await _unitOfWork.DropoffRequests
                .GetAllAsync(dr => dr.OrganizationId == user.OrganizationId,
                    includeProperties: "Consignor");

            var result = requests.OrderByDescending(dr => dr.CreatedAt)
                .Select(dr => new OwnerDropoffRequestListDto
                {
                    Id = dr.Id,
                    ItemCount = dr.ItemCount,
                    SuggestedTotal = dr.SuggestedTotal,
                    PlannedDate = dr.PlannedDate,
                    PlannedTimeSlot = dr.PlannedTimeSlot,
                    Status = dr.Status,
                    CreatedAt = dr.CreatedAt,
                    ImportedAt = dr.ImportedAt,
                    Consignor = new ConsignorSummaryDto
                    {
                        Id = dr.Consignor.Id,
                        Name = dr.Consignor.Name,
                        PreferredName = dr.Consignor.PreferredName,
                        Email = dr.Consignor.Email,
                        Phone = dr.Consignor.Phone
                    }
                })
                .ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dropoff requests");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("dropoff-requests/{id}")]
    public async Task<ActionResult<OwnerDropoffRequestDetailDto>> GetDropoffRequest(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return BadRequest("User not found");

            var user = await _unitOfWork.Users
                .GetAsync(u => u.Id == userId.Value, includeProperties: "Organization");

            if (user == null)
                return NotFound("User not found");

            var dropoffRequest = await _unitOfWork.DropoffRequests
                .GetAsync(dr => dr.Id == id && dr.OrganizationId == user.OrganizationId,
                    includeProperties: "Consignor,Organization");

            if (dropoffRequest == null)
                return NotFound("Dropoff request not found");

            var result = new OwnerDropoffRequestDetailDto
            {
                Id = dropoffRequest.Id,
                ItemCount = dropoffRequest.ItemCount,
                SuggestedTotal = dropoffRequest.SuggestedTotal,
                MinimumTotal = dropoffRequest.MinimumTotal,
                PlannedDate = dropoffRequest.PlannedDate,
                PlannedTimeSlot = dropoffRequest.PlannedTimeSlot,
                Status = dropoffRequest.Status,
                CreatedAt = dropoffRequest.CreatedAt,
                ImportedAt = dropoffRequest.ImportedAt,
                ReceivedAt = dropoffRequest.ReceivedAt,
                RejectedAt = dropoffRequest.RejectedAt,
                RejectionReason = dropoffRequest.RejectionReason,
                ReopenedAt = dropoffRequest.ReopenedAt,
                PhotosPurgedAt = dropoffRequest.PhotosPurgedAt,
                OwnerNotes = dropoffRequest.OwnerNotes,
                Items = dropoffRequest.GetItems(),
                Message = dropoffRequest.ConsignorMessage,
                Shop = new ShopSummaryDto
                {
                    Name = dropoffRequest.Organization.ShopName ?? dropoffRequest.Organization.Name,
                    Address = FormatShopAddress(dropoffRequest.Organization),
                    Phone = dropoffRequest.Organization.ShopPhone
                },
                Consignor = new ConsignorSummaryDto
                {
                    Id = dropoffRequest.Consignor.Id,
                    Name = dropoffRequest.Consignor.Name,
                    PreferredName = dropoffRequest.Consignor.PreferredName,
                    Email = dropoffRequest.Consignor.Email,
                    Phone = dropoffRequest.Consignor.Phone
                }
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dropoff request detail");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("dropoff-requests/{id}/status")]
    public async Task<ActionResult> UpdateDropoffRequestStatus(Guid id,
        [FromBody] UpdateDropoffStatusDto request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return BadRequest("User not found");

            var user = await _unitOfWork.Users
                .GetAsync(u => u.Id == userId.Value, includeProperties: "Organization");

            if (user == null)
                return NotFound("User not found");

            var dropoffRequest = await _unitOfWork.DropoffRequests
                .GetAsync(dr => dr.Id == id && dr.OrganizationId == user.OrganizationId);

            if (dropoffRequest == null)
                return NotFound("Dropoff request not found");

            dropoffRequest.Status = request.Status;
            dropoffRequest.OwnerNotes = request.OwnerNotes;
            dropoffRequest.UpdatedAt = DateTime.UtcNow;

            if (request.Status == DropoffRequestStatus.Received)
                dropoffRequest.ReceivedAt = DateTime.UtcNow;

            await _unitOfWork.DropoffRequests.UpdateAsync(dropoffRequest);
            await _unitOfWork.SaveChangesAsync();

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating dropoff request status");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("dropoff-requests/{id}/import")]
    public async Task<ActionResult> ImportDropoffRequest(Guid id,
        [FromBody] ImportDropoffRequestDto request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return BadRequest("User not found");

            var user = await _unitOfWork.Users
                .GetAsync(u => u.Id == userId.Value, includeProperties: "Organization");

            if (user == null)
                return NotFound("User not found");

            var dropoffRequest = await _unitOfWork.DropoffRequests
                .GetAsync(dr => dr.Id == id && dr.OrganizationId == user.OrganizationId);

            if (dropoffRequest == null)
                return NotFound("Dropoff request not found");

            if (dropoffRequest.Status != DropoffRequestStatus.Received)
                return BadRequest("Can only import received dropoff requests");

            // Create pending import items from the manifest
            var createPendingImportsRequest = new CreatePendingImportsFromManifestRequest
            {
                ManifestId = id,
                AutoAssignConsignor = true
            };

            var pendingImportItems = await _pendingImportItemService.CreateFromManifestAsync(user.OrganizationId, createPendingImportsRequest);

            // Update dropoff request with owner notes
            dropoffRequest.OwnerNotes = request.OwnerNotes;
            dropoffRequest.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.DropoffRequests.UpdateAsync(dropoffRequest);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Successfully imported {ItemCount} items from dropoff request {DropoffRequestId}",
                pendingImportItems.Count, id);

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing dropoff request");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("dropoff-requests/{id}/reject")]
    public async Task<ActionResult> RejectDropoffRequest(Guid id,
        [FromBody] RejectDropoffRequestDto request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return BadRequest("User not found");

            var user = await _unitOfWork.Users
                .GetAsync(u => u.Id == userId.Value, includeProperties: "Organization");

            if (user == null)
                return NotFound("User not found");

            var dropoffRequest = await _unitOfWork.DropoffRequests
                .GetAsync(dr => dr.Id == id && dr.OrganizationId == user.OrganizationId);

            if (dropoffRequest == null)
                return NotFound("Dropoff request not found");

            if (dropoffRequest.Status == DropoffRequestStatus.Rejected)
                return BadRequest("Dropoff request is already rejected");

            if (dropoffRequest.Status == DropoffRequestStatus.Imported)
                return BadRequest("Cannot reject imported dropoff requests");

            dropoffRequest.Status = DropoffRequestStatus.Rejected;
            dropoffRequest.RejectedAt = DateTime.UtcNow;
            dropoffRequest.RejectionReason = request.RejectionReason;
            dropoffRequest.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.DropoffRequests.UpdateAsync(dropoffRequest);
            await _unitOfWork.SaveChangesAsync();

            return Ok(new { success = true, message = "Dropoff request rejected successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting dropoff request");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("dropoff-requests/{id}/reopen")]
    public async Task<ActionResult> ReopenDropoffRequest(Guid id,
        [FromBody] ReopenDropoffRequestDto request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return BadRequest("User not found");

            var user = await _unitOfWork.Users
                .GetAsync(u => u.Id == userId.Value, includeProperties: "Organization");

            if (user == null)
                return NotFound("User not found");

            var dropoffRequest = await _unitOfWork.DropoffRequests
                .GetAsync(dr => dr.Id == id && dr.OrganizationId == user.OrganizationId);

            if (dropoffRequest == null)
                return NotFound("Dropoff request not found");

            if (dropoffRequest.Status != DropoffRequestStatus.Rejected)
                return BadRequest("Only rejected dropoff requests can be reopened");

            dropoffRequest.Status = DropoffRequestStatus.Pending;
            dropoffRequest.ReopenedAt = DateTime.UtcNow;
            dropoffRequest.OwnerNotes = request.OwnerNotes;
            dropoffRequest.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.DropoffRequests.UpdateAsync(dropoffRequest);
            await _unitOfWork.SaveChangesAsync();

            return Ok(new { success = true, message = "Dropoff request reopened successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reopening dropoff request");
            return StatusCode(500, "Internal server error");
        }
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private string? FormatShopAddress(Organization organization)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(organization.ShopAddress1))
            parts.Add(organization.ShopAddress1);

        if (!string.IsNullOrWhiteSpace(organization.ShopAddress2))
            parts.Add(organization.ShopAddress2);

        var cityStateZip = new List<string>();
        if (!string.IsNullOrWhiteSpace(organization.ShopCity))
            cityStateZip.Add(organization.ShopCity);

        if (!string.IsNullOrWhiteSpace(organization.ShopState))
            cityStateZip.Add(organization.ShopState);

        if (!string.IsNullOrWhiteSpace(organization.ShopZip))
            cityStateZip.Add(organization.ShopZip);

        if (cityStateZip.Any())
            parts.Add(string.Join(", ", cityStateZip));

        return parts.Any() ? string.Join(", ", parts) : null;
    }
}