using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Application.Services;
using ConsignmentGenie.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/pending-imports")]
[Authorize]
public class PendingImportsController : ControllerBase
{
    private readonly IPendingImportItemService _pendingImportItemService;
    private readonly ILogger<PendingImportsController> _logger;

    public PendingImportsController(
        IPendingImportItemService pendingImportItemService,
        ILogger<PendingImportsController> logger)
    {
        _pendingImportItemService = pendingImportItemService;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated list of pending import items
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<PendingImportItemDto>>> GetPendingImports([FromQuery] PendingImportItemsQueryParams queryParams)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var result = await _pendingImportItemService.GetPendingImportItemsAsync(organizationId, queryParams);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending imports");
            return StatusCode(500, "An error occurred while retrieving pending imports");
        }
    }

    /// <summary>
    /// Get specific pending import item by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<PendingImportItemDto>> GetPendingImport(Guid id)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var result = await _pendingImportItemService.GetPendingImportItemAsync(organizationId, id);

            if (result == null)
                return NotFound();

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending import {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the pending import");
        }
    }

    /// <summary>
    /// Create a new pending import item
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<PendingImportItemDto>> CreatePendingImport([FromBody] CreatePendingImportItemRequest request)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown";

            _logger.LogInformation("🔄 [HTTP->PendingImport] SINGLE CREATE REQUEST - Name: {Name}, Source: {Source}, SourceRef: {SourceRef}, OrgId: {OrgId}, UserId: {UserId}, ConsignorId: {ConsignorId}",
                request.Name, request.Source, request.SourceReference, organizationId, userId, request.ConsignorId);

            var result = await _pendingImportItemService.CreatePendingImportItemAsync(organizationId, request);

            _logger.LogInformation("✅ [HTTP->PendingImport] SINGLE CREATE COMPLETED - ID: {ResultId}, Name: {Name}", result.Id, result.Name);

            return CreatedAtAction(nameof(GetPendingImport), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating pending import");
            return StatusCode(500, "An error occurred while creating the pending import");
        }
    }

    /// <summary>
    /// Create multiple pending import items
    /// </summary>
    [HttpPost("bulk")]
    public async Task<ActionResult<List<PendingImportItemDto>>> CreatePendingImportsBulk([FromBody] List<CreatePendingImportItemRequest> requests)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown";

            _logger.LogInformation("🔄 [HTTP->PendingImport] BULK CREATE REQUEST - Count: {Count}, OrgId: {OrgId}, UserId: {UserId}, Sources: [{Sources}]",
                requests.Count, organizationId, userId, string.Join(", ", requests.Select(r => r.Source).Distinct()));

            var result = await _pendingImportItemService.CreatePendingImportItemsBulkAsync(organizationId, requests);

            _logger.LogInformation("✅ [HTTP->PendingImport] BULK CREATE COMPLETED - Count: {Count}, IDs: [{ResultIds}]",
                result.Count, string.Join(", ", result.Select(r => r.Id)));

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating bulk pending imports");
            return StatusCode(500, "An error occurred while creating bulk pending imports");
        }
    }

    /// <summary>
    /// Update an existing pending import item
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<PendingImportItemDto>> UpdatePendingImport(Guid id, [FromBody] UpdatePendingImportItemRequest request)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var result = await _pendingImportItemService.UpdatePendingImportItemAsync(organizationId, id, request);

            if (result == null)
                return NotFound();

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating pending import {Id}", id);
            return StatusCode(500, "An error occurred while updating the pending import");
        }
    }

    /// <summary>
    /// Partial update of pending import item (for inline editing)
    /// </summary>
    [HttpPatch("{id}")]
    public async Task<ActionResult<PendingImportItemDto>> PatchPendingImport(Guid id, [FromBody] PatchPendingImportItemRequest request)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var result = await _pendingImportItemService.PatchPendingImportItemAsync(organizationId, id, request);

            if (result == null)
                return NotFound();

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error patching pending import {Id}", id);
            return StatusCode(500, "An error occurred while updating the pending import");
        }
    }

    /// <summary>
    /// Delete a pending import item
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeletePendingImport(Guid id)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var currentUserId = GetUserId();
            var success = await _pendingImportItemService.DeletePendingImportItemAsync(organizationId, id, currentUserId);

            if (!success)
                return NotFound();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting pending import {Id}", id);
            return StatusCode(500, "An error occurred while deleting the pending import");
        }
    }

    /// <summary>
    /// Delete multiple pending import items
    /// </summary>
    [HttpDelete("bulk")]
    public async Task<ActionResult<int>> DeletePendingImportsBulk([FromBody] List<Guid> ids)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var deletedCount = await _pendingImportItemService.DeletePendingImportItemsBulkAsync(organizationId, ids);
            return Ok(deletedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting bulk pending imports");
            return StatusCode(500, "An error occurred while deleting bulk pending imports");
        }
    }

    /// <summary>
    /// Assign consignor to a pending import item
    /// </summary>
    [HttpPost("{id}/assign")]
    public async Task<ActionResult<PendingImportItemDto>> AssignConsignor(Guid id, [FromBody] AssignConsignorToPendingImportRequest request)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var result = await _pendingImportItemService.AssignConsignorAsync(organizationId, id, request.ConsignorId);

            if (result == null)
                return NotFound();

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning consignor to pending import {Id}", id);
            return StatusCode(500, "An error occurred while assigning consignor");
        }
    }

    /// <summary>
    /// Bulk assign consignor to multiple pending import items
    /// </summary>
    [HttpPost("assign/bulk")]
    public async Task<ActionResult<BulkAssignPendingImportsResult>> BulkAssignConsignor([FromBody] ConsignmentGenie.Application.DTOs.BulkAssignPendingImportsRequest request)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var result = await _pendingImportItemService.BulkAssignConsignorAsync(organizationId, request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk assigning consignor");
            return StatusCode(500, "An error occurred while bulk assigning consignor");
        }
    }



    /// <summary>
    /// Import verified pending items as actual inventory items
    /// </summary>
    [HttpPost("import")]
    public async Task<ActionResult<ImportPendingItemsResult>> ImportVerifiedItems([FromBody] ImportVerifiedItemsRequest request)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var result = await _pendingImportItemService.ImportVerifiedItemsAsync(organizationId, request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing verified items");
            return StatusCode(500, "An error occurred while importing verified items");
        }
    }

    /// <summary>
    /// Import a single pending item as actual inventory item
    /// </summary>
    [HttpPost("{id}/import")]
    public async Task<ActionResult<ImportPendingItemsResult>> ImportSingleItem(Guid id)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var request = new ImportVerifiedItemsRequest { PendingImportIds = new List<Guid> { id } };
            var result = await _pendingImportItemService.ImportVerifiedItemsAsync(organizationId, request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing single item {Id}", id);
            return StatusCode(500, "An error occurred while importing the item");
        }
    }

    /// <summary>
    /// Create pending import items from a manifest (dropoff request)
    /// </summary>
    [HttpPost("from-manifest/{manifestId}")]
    public async Task<ActionResult<List<PendingImportItemDto>>> CreateFromManifest(Guid manifestId, [FromBody] CreatePendingImportsFromManifestRequest? request = null)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown";
            var createRequest = request ?? new CreatePendingImportsFromManifestRequest { ManifestId = manifestId, AutoAssignConsignor = true };
            createRequest.ManifestId = manifestId; // Ensure the ID from route is used

            _logger.LogInformation("🔄 [HTTP->PendingImport] MANIFEST CREATE REQUEST - ManifestId: {ManifestId}, OrgId: {OrgId}, UserId: {UserId}, AutoAssign: {AutoAssign}",
                manifestId, organizationId, userId, createRequest.AutoAssignConsignor);

            var result = await _pendingImportItemService.CreateFromManifestAsync(organizationId, createRequest);

            _logger.LogInformation("✅ [HTTP->PendingImport] MANIFEST CREATE COMPLETED - Count: {Count}, ManifestId: {ManifestId}",
                result.Count, manifestId);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid manifest ID {ManifestId}", manifestId);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating pending imports from manifest {ManifestId}", manifestId);
            return StatusCode(500, "An error occurred while creating pending imports from manifest");
        }
    }

    /// <summary>
    /// Create pending import items from CSV upload
    /// </summary>
    [HttpPost("from-csv")]
    public async Task<ActionResult<List<PendingImportItemDto>>> CreateFromCsvUpload([FromBody] CreatePendingImportsFromCsvRequest request)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown";

            _logger.LogInformation("🔄 [HTTP->PendingImport] CSV CREATE REQUEST - FileName: {FileName}, ItemCount: {ItemCount}, OrgId: {OrgId}, UserId: {UserId}",
                request.FileName, request.Items.Count, organizationId, userId);

            var result = await _pendingImportItemService.CreateFromCsvAsync(organizationId, request);

            _logger.LogInformation("✅ [HTTP->PendingImport] CSV CREATE COMPLETED - Count: {Count}, FileName: {FileName}",
                result.Count, request.FileName);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating pending imports from CSV");
            return StatusCode(500, "An error occurred while creating pending imports from CSV");
        }
    }

    private Guid GetOrganizationId()
    {
        // Use standard organizationId claim (lowercase following JWT conventions)
        var orgIdClaim = User.FindFirst("organizationId")?.Value;

        if (Guid.TryParse(orgIdClaim, out var orgId))
        {
            return orgId;
        }

        throw new UnauthorizedAccessException("Organization ID not found in token");
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        throw new UnauthorizedAccessException("User ID not found in token");
    }
}