using ConsignmentGenie.Application.Models;
using ConsignmentGenie.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "owner")]
public class PhotosController : ControllerBase
{
    private readonly IPhotoService _photoService;

    public PhotosController(IPhotoService photoService)
    {
        _photoService = photoService;
    }

    private Guid GetOrganizationId()
    {
        var organizationIdClaim = User.FindFirst("organizationId")?.Value;
        return Guid.TryParse(organizationIdClaim, out var orgId) ? orgId : Guid.Empty;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10MB limit
    public async Task<ActionResult<object>> UploadPhoto(IFormFile file, [FromForm] Guid itemId)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded");
            }

            // Validate file size
            const long maxFileSize = 10 * 1024 * 1024; // 10MB
            if (file.Length > maxFileSize)
            {
                return BadRequest("File size exceeds 10MB limit");
            }

            // Validate file type
            var allowedContentTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp" };
            if (!allowedContentTypes.Contains(file.ContentType.ToLower()))
            {
                return BadRequest("Invalid file type. Only JPEG, PNG, and WEBP are supported");
            }

            var organizationId = GetOrganizationId();

            using var stream = file.OpenReadStream();
            var photoUrl = await _photoService.UploadPhotoAsync(organizationId, itemId, stream, file.FileName);

            return Ok(new { success = true, data = photoUrl, message = "Photo uploaded successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to upload photo: {ex.Message}");
        }
    }

    [HttpDelete("{photoId}")]
    public async Task<ActionResult<object>> DeletePhoto(string photoId)
    {
        try
        {
            // Decode the photo URL from the photoId parameter
            var photoUrl = Uri.UnescapeDataString(photoId);
            var organizationId = GetOrganizationId();

            var result = await _photoService.DeletePhotoAsync(organizationId, photoUrl);

            if (result)
            {
                return Ok(new { success = true, message = "Photo deleted successfully" });
            }
            else
            {
                return NotFound("Photo not found");
            }
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to delete photo: {ex.Message}");
        }
    }
}

[ApiController]
[Route("api/items")]
[Authorize(Roles = "owner")]
public class ItemPhotosController : ControllerBase
{
    private readonly IPhotoService _photoService;

    public ItemPhotosController(IPhotoService photoService)
    {
        _photoService = photoService;
    }

    [HttpGet("{itemId}/photos")]
    public async Task<ActionResult<List<PhotoInfo>>> GetItemPhotos(Guid itemId)
    {
        try
        {
            var photos = await _photoService.GetPhotosAsync(itemId);
            return Ok(photos);
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to get item photos: {ex.Message}");
        }
    }
}