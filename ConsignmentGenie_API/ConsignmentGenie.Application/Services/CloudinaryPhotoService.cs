using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using ConsignmentGenie.Application.Models;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ConsignmentGenie.Application.Services;

public class CloudinaryPhotoService : IPhotoService
{
    private readonly Cloudinary _cloudinary;
    private readonly ConsignmentGenieContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CloudinaryPhotoService> _logger;

    public CloudinaryPhotoService(ConsignmentGenieContext context, IConfiguration configuration, ILogger<CloudinaryPhotoService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;

        // Initialize Cloudinary
        var cloudinaryUrl = _configuration["Cloudinary:Url"];
        if (!string.IsNullOrEmpty(cloudinaryUrl))
        {
            _cloudinary = new Cloudinary(cloudinaryUrl);
        }
        else
        {
            var account = new Account(
                _configuration["Cloudinary:CloudName"],
                _configuration["Cloudinary:ApiKey"],
                _configuration["Cloudinary:ApiSecret"]
            );
            _cloudinary = new Cloudinary(account);
        }
    }

    public async Task<string> UploadPhotoAsync(Guid organizationId, Guid itemId, Stream fileStream, string fileName)
    {
        try
        {
            // Validate file size (10MB max)
            const long maxFileSize = 10 * 1024 * 1024; // 10MB
            if (fileStream.Length > maxFileSize)
            {
                throw new InvalidOperationException("File size exceeds 10MB limit");
            }

            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                throw new InvalidOperationException("Invalid file type. Only JPEG, PNG, and WEBP are supported");
            }

            // Generate unique public ID for the image
            var publicId = $"consignment/{organizationId}/{itemId}/{Guid.NewGuid()}";

            // Upload to Cloudinary
            var uploadParams = new ImageUploadParams()
            {
                File = new FileDescription(fileName, fileStream),
                PublicId = publicId,
                Transformation = new Transformation()
                    .Quality("auto:good")
                    .FetchFormat("auto"),
                Folder = $"consignment/{organizationId}/{itemId}",
                Tags = "consignment,item-photo," + organizationId.ToString() + "," + itemId.ToString()
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
            {
                throw new InvalidOperationException($"Cloudinary upload failed: {uploadResult.Error.Message}");
            }

            var photoUrl = uploadResult.SecureUrl.ToString();

            // Update item's photos JSON
            await UpdateItemPhotosAsync(itemId, photoUrl);

            _logger.LogInformation("Photo uploaded successfully to Cloudinary: {PhotoUrl}", photoUrl);
            return photoUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload photo for item {ItemId}", itemId);
            throw;
        }
    }

    public async Task<bool> DeletePhotoAsync(Guid organizationId, string photoUrl)
    {
        try
        {
            // Extract public ID from Cloudinary URL
            var uri = new Uri(photoUrl);
            var pathParts = uri.AbsolutePath.Split('/');

            // Find the public ID (everything after /upload/version/)
            var uploadIndex = Array.IndexOf(pathParts, "upload");
            if (uploadIndex == -1 || uploadIndex + 2 >= pathParts.Length)
            {
                throw new InvalidOperationException("Invalid Cloudinary URL format");
            }

            // Skip the version number and get the public ID
            var publicIdParts = pathParts.Skip(uploadIndex + 2);
            var publicId = string.Join("/", publicIdParts);

            // Remove file extension if present
            if (publicId.Contains('.'))
            {
                publicId = publicId.Substring(0, publicId.LastIndexOf('.'));
            }

            // Delete from Cloudinary
            var deleteParams = new DeletionParams(publicId);
            var result = await _cloudinary.DestroyAsync(deleteParams);

            var success = result.Result == "ok" || result.Result == "not found";

            if (success)
            {
                _logger.LogInformation("Photo deleted successfully from Cloudinary: {PhotoUrl}", photoUrl);
            }
            else
            {
                _logger.LogWarning("Failed to delete photo from Cloudinary: {PhotoUrl}, Result: {Result}", photoUrl, result.Result);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete photo {PhotoUrl}", photoUrl);
            return false;
        }
    }

    public async Task<string> GenerateThumbnailAsync(string photoUrl)
    {
        try
        {
            // With Cloudinary, we can generate thumbnails on-the-fly using transformations
            var uri = new Uri(photoUrl);
            var pathParts = uri.AbsolutePath.Split('/');

            var uploadIndex = Array.IndexOf(pathParts, "upload");
            if (uploadIndex == -1)
            {
                throw new InvalidOperationException("Invalid Cloudinary URL format");
            }

            // Insert thumbnail transformation after "upload"
            var baseParts = pathParts.Take(uploadIndex + 1).ToList();
            baseParts.Add("c_thumb,w_200,h_200,g_center,q_auto:good,f_auto");
            baseParts.AddRange(pathParts.Skip(uploadIndex + 1));

            var thumbnailUrl = $"{uri.Scheme}://{uri.Host}{string.Join("/", baseParts)}";

            _logger.LogDebug("Generated thumbnail URL: {ThumbnailUrl}", thumbnailUrl);
            return await Task.FromResult(thumbnailUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate thumbnail for {PhotoUrl}", photoUrl);
            return await Task.FromResult(string.Empty);
        }
    }

    public async Task<List<PhotoInfo>> GetPhotosAsync(Guid itemId)
    {
        var item = await _context.Items.FindAsync(itemId);
        if (item == null || string.IsNullOrEmpty(item.Photos))
        {
            return new List<PhotoInfo>();
        }

        try
        {
            var photoUrls = System.Text.Json.JsonSerializer.Deserialize<string[]>(item.Photos) ?? Array.Empty<string>();
            var photos = new List<PhotoInfo>();

            foreach (var url in photoUrls)
            {
                var thumbnailUrl = await GenerateThumbnailAsync(url);
                photos.Add(new PhotoInfo
                {
                    Url = url,
                    ThumbnailUrl = thumbnailUrl,
                    FileName = GetFileNameFromCloudinaryUrl(url),
                    UploadedAt = item.CreatedAt
                });
            }

            return photos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get photos for item {ItemId}", itemId);
            return new List<PhotoInfo>();
        }
    }

    private async Task UpdateItemPhotosAsync(Guid itemId, string photoUrl)
    {
        var item = await _context.Items.FindAsync(itemId);
        if (item == null) return;

        var currentPhotos = string.IsNullOrEmpty(item.Photos)
            ? new List<string>()
            : System.Text.Json.JsonSerializer.Deserialize<List<string>>(item.Photos) ?? new List<string>();

        currentPhotos.Add(photoUrl);

        // Enforce 5 photo limit
        if (currentPhotos.Count > 5)
        {
            currentPhotos = currentPhotos.TakeLast(5).ToList();
        }

        item.Photos = System.Text.Json.JsonSerializer.Serialize(currentPhotos);
        await _context.SaveChangesAsync();
    }

    private string GetFileNameFromCloudinaryUrl(string photoUrl)
    {
        try
        {
            var uri = new Uri(photoUrl);
            var pathParts = uri.AbsolutePath.Split('/');
            return pathParts.LastOrDefault() ?? "photo";
        }
        catch
        {
            return "photo";
        }
    }
}