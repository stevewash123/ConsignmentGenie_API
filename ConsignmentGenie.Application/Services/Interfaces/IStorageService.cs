namespace ConsignmentGenie.Application.Services.Interfaces;

/// <summary>
/// Storage service abstraction for file/image uploads
/// MVP: LocalStorageService (dev), AzureBlobStorageService (production)
/// Future: S3StorageService, etc.
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Upload a file and return the accessible URL
    /// </summary>
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType);

    /// <summary>
    /// Delete a file by URL or key
    /// </summary>
    Task DeleteFileAsync(string fileUrlOrKey);

    /// <summary>
    /// Get a signed/temporary URL for secure access (if supported)
    /// </summary>
    Task<string> GetSecureUrlAsync(string fileUrlOrKey, TimeSpan expiry);

    /// <summary>
    /// Check if a file exists
    /// </summary>
    Task<bool> FileExistsAsync(string fileUrlOrKey);
}