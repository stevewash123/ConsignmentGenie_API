using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Application.Models;
using ConsignmentGenie.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Owner")]
public class PhotosController : ControllerBase
{
    private readonly IPhotoService _photoService;

    public PhotosController(IPhotoService photoService)
    {
        _photoService = photoService;
    }

    private Guid GetOrganizationId()
    {
        var organizationIdClaim = User.FindFirst("OrganizationId")?.Value;
        return Guid.TryParse(organizationIdClaim, out var orgId) ? orgId : Guid.Empty;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10MB limit
    public async Task<ActionResult<ApiResponse<string>>> UploadPhoto(IFormFile file, [FromForm] Guid itemId)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(ApiResponse<string>.ErrorResult("No file uploaded"));
            }

            // Validate file size
            const long maxFileSize = 10 * 1024 * 1024; // 10MB
            if (file.Length > maxFileSize)
            {
                return BadRequest(ApiResponse<string>.ErrorResult("File size exceeds 10MB limit"));
            }

            // Validate file type
            var allowedContentTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp" };
            if (!allowedContentTypes.Contains(file.ContentType.ToLower()))
            {
                return BadRequest(ApiResponse<string>.ErrorResult("Invalid file type. Only JPEG, PNG, and WEBP are supported"));
            }

            var organizationId = GetOrganizationId();

            using var stream = file.OpenReadStream();
            var photoUrl = await _photoService.UploadPhotoAsync(organizationId, itemId, stream, file.FileName);

            return Ok(ApiResponse<string>.SuccessResult(photoUrl, "Photo uploaded successfully"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<string>.ErrorResult($"Failed to upload photo: {ex.Message}"));
        }
    }

    [HttpDelete("{photoId}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeletePhoto(string photoId)
    {
        try
        {
            // Decode the photo URL from the photoId parameter
            var photoUrl = Uri.UnescapeDataString(photoId);
            var organizationId = GetOrganizationId();

            var result = await _photoService.DeletePhotoAsync(organizationId, photoUrl);

            if (result)
            {
                return Ok(ApiResponse<bool>.SuccessResult(true, "Photo deleted successfully"));
            }
            else
            {
                return NotFound(ApiResponse<bool>.ErrorResult("Photo not found"));
            }
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<bool>.ErrorResult($"Failed to delete photo: {ex.Message}"));
        }
    }
}

[ApiController]
[Route("api/items")]
[Authorize(Roles = "Owner")]
public class ItemPhotosController : ControllerBase
{
    private readonly IPhotoService _photoService;

    public ItemPhotosController(IPhotoService photoService)
    {
        _photoService = photoService;
    }

    [HttpGet("{itemId}/photos")]
    public async Task<ActionResult<ApiResponse<List<PhotoInfo>>>> GetItemPhotos(Guid itemId)
    {
        try
        {
            var photos = await _photoService.GetPhotosAsync(itemId);
            return Ok(ApiResponse<List<PhotoInfo>>.SuccessResult(photos));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<List<PhotoInfo>>.ErrorResult($"Failed to get item photos: {ex.Message}"));
        }
    }
}