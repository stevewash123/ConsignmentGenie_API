using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ConsignmentGenie.Application.Models;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace ConsignmentGenie.Application.Services;

public class AzurePhotoService : IPhotoService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ConsignmentGenieContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzurePhotoService> _logger;
    private readonly string _containerName;

    public AzurePhotoService(BlobServiceClient blobServiceClient, ConsignmentGenieContext context, IConfiguration configuration, ILogger<AzurePhotoService> logger)
    {
        _blobServiceClient = blobServiceClient;
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _containerName = _configuration["Azure:Storage:ContainerName"] ?? "consignmentpro-photos";
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

            // Generate unique filename
            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var blobPath = $"{organizationId}/{itemId}/{uniqueFileName}";

            // Get blob container
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            // Upload file
            var blobClient = containerClient.GetBlobClient(blobPath);

            var blobHttpHeaders = new BlobHttpHeaders
            {
                ContentType = GetContentType(extension)
            };

            fileStream.Position = 0;
            await blobClient.UploadAsync(fileStream, new BlobUploadOptions
            {
                HttpHeaders = blobHttpHeaders
            });

            var photoUrl = blobClient.Uri.ToString();

            // Generate thumbnail
            var thumbnailUrl = await GenerateThumbnailAsync(photoUrl);

            // Update item's photos JSON
            await UpdateItemPhotosAsync(itemId, photoUrl);

            _logger.LogInformation("Photo uploaded successfully to Azure: {PhotoUrl}", photoUrl);
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
            // Extract blob path from URL
            var uri = new Uri(photoUrl);
            var blobPath = uri.AbsolutePath.TrimStart('/');

            // Remove container name from path if present
            if (blobPath.StartsWith(_containerName + "/"))
            {
                blobPath = blobPath.Substring(_containerName.Length + 1);
            }

            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);

            // Delete main photo
            var blobClient = containerClient.GetBlobClient(blobPath);
            await blobClient.DeleteIfExistsAsync();

            // Delete thumbnail
            var thumbnailPath = GetThumbnailPath(blobPath);
            var thumbnailClient = containerClient.GetBlobClient(thumbnailPath);
            await thumbnailClient.DeleteIfExistsAsync();

            _logger.LogInformation("Photo deleted successfully from Azure: {PhotoUrl}", photoUrl);
            return true;
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
            // Extract blob path from URL
            var uri = new Uri(photoUrl);
            var blobPath = uri.AbsolutePath.TrimStart('/');

            if (blobPath.StartsWith(_containerName + "/"))
            {
                blobPath = blobPath.Substring(_containerName.Length + 1);
            }

            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(blobPath);

            // Download original image
            var response = await blobClient.DownloadAsync();

            using var originalStream = response.Value.Content;
            using var thumbnailStream = new MemoryStream();

            // Create thumbnail (200x200px)
            using (var image = await Image.LoadAsync(originalStream))
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(200, 200),
                    Mode = ResizeMode.Crop
                }));

                await image.SaveAsJpegAsync(thumbnailStream);
            }

            // Upload thumbnail
            var thumbnailPath = GetThumbnailPath(blobPath);
            var thumbnailClient = containerClient.GetBlobClient(thumbnailPath);

            thumbnailStream.Position = 0;
            await thumbnailClient.UploadAsync(thumbnailStream, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = "image/jpeg" }
            });

            return thumbnailClient.Uri.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate thumbnail for {PhotoUrl}", photoUrl);
            return string.Empty;
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
                var thumbnailUrl = GetThumbnailUrlFromPhotoUrl(url);
                photos.Add(new PhotoInfo
                {
                    Url = url,
                    ThumbnailUrl = thumbnailUrl,
                    FileName = Path.GetFileName(new Uri(url).AbsolutePath),
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

    private string GetThumbnailPath(string originalPath)
    {
        var directory = Path.GetDirectoryName(originalPath);
        var filename = Path.GetFileNameWithoutExtension(originalPath);
        return $"{directory}/thumbnails/{filename}_thumb.jpg";
    }

    private string GetThumbnailUrlFromPhotoUrl(string photoUrl)
    {
        var uri = new Uri(photoUrl);
        var path = uri.AbsolutePath;
        var directory = Path.GetDirectoryName(path);
        var filename = Path.GetFileNameWithoutExtension(path);
        var thumbnailPath = $"{directory}/thumbnails/{filename}_thumb.jpg";
        return new UriBuilder(uri) { Path = thumbnailPath }.ToString();
    }

    private string GetContentType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }
}