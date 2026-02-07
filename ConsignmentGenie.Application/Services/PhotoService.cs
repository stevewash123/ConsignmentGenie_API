using ConsignmentGenie.Application.Models;
using ConsignmentGenie.Application.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ConsignmentGenie.Application.Services;

public class PhotoService : IPhotoService
{
    private readonly IPhotoService _implementation;
    private readonly ILogger<PhotoService> _logger;

    public PhotoService(IServiceProvider serviceProvider, IConfiguration configuration, ILogger<PhotoService> logger)
    {
        _logger = logger;

        // Determine which storage provider to use based on configuration
        var photoStorageProvider = configuration["PhotoStorage:Consignor"]?.ToLowerInvariant() ?? "cloudinary";

        _implementation = photoStorageProvider switch
        {
            "azure" => serviceProvider.GetRequiredService<AzurePhotoService>(),
            "cloudinary" => serviceProvider.GetRequiredService<CloudinaryPhotoService>(),
            _ => throw new InvalidOperationException($"Unsupported photo storage provider: {photoStorageProvider}")
        };

        _logger.LogInformation("Photo service initialized with provider: {Consignor}", photoStorageProvider);
    }

    public async Task<string> UploadPhotoAsync(Guid organizationId, Guid itemId, Stream fileStream, string fileName)
    {
        return await _implementation.UploadPhotoAsync(organizationId, itemId, fileStream, fileName);
    }

    public async Task<bool> DeletePhotoAsync(Guid organizationId, string photoUrl)
    {
        return await _implementation.DeletePhotoAsync(organizationId, photoUrl);
    }

    public async Task<string> GenerateThumbnailAsync(string photoUrl)
    {
        return await _implementation.GenerateThumbnailAsync(photoUrl);
    }

    public async Task<List<PhotoInfo>> GetPhotosAsync(Guid itemId)
    {
        return await _implementation.GetPhotosAsync(itemId);
    }
}