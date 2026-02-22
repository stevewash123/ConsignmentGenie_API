using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using ConsignmentGenie.Application.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ConsignmentGenie.Application.Services;

/// <summary>
/// Cloudinary-based document service for agreement templates and signed agreements
/// </summary>
public class CloudinaryDocumentService : IDocumentService
{
    private readonly Cloudinary _cloudinary;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CloudinaryDocumentService> _logger;

    public CloudinaryDocumentService(IConfiguration configuration, ILogger<CloudinaryDocumentService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        // Initialize Cloudinary (same pattern as CloudinaryPhotoService)
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

    public async Task<string> UploadAgreementTemplateAsync(Guid organizationId, Stream fileStream, string fileName)
    {
        try
        {
            // Validate file size (50MB max for documents)
            const long maxFileSize = 50 * 1024 * 1024; // 50MB
            if (fileStream.Length > maxFileSize)
            {
                throw new InvalidOperationException("File size exceeds 50MB limit");
            }

            // Validate file type (documents)
            var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".txt" };
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                throw new InvalidOperationException("Invalid file type. Only PDF, DOC, DOCX, and TXT files are supported");
            }

            // Generate unique public ID for the agreement template (just the filename part)
            var fileId = Guid.NewGuid().ToString();

            // Upload to Cloudinary as raw file (not image)
            var uploadParams = new RawUploadParams()
            {
                File = new FileDescription(fileName, fileStream),
                PublicId = fileId,
                Folder = $"agreements/templates/{organizationId}",
                Tags = $"agreement,template,{organizationId}"
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
            {
                throw new InvalidOperationException($"Cloudinary upload failed: {uploadResult.Error.Message}");
            }

            var documentUrl = uploadResult.SecureUrl.ToString();
            _logger.LogInformation("Agreement template uploaded successfully to Cloudinary: {DocumentUrl}", documentUrl);
            return documentUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload agreement template for organization {OrganizationId}", organizationId);
            throw;
        }
    }

    public async Task<string> UploadSignedAgreementAsync(Guid organizationId, Guid consignorId, Stream fileStream, string fileName)
    {
        try
        {
            // Validate file size (50MB max for documents)
            const long maxFileSize = 50 * 1024 * 1024; // 50MB
            if (fileStream.Length > maxFileSize)
            {
                throw new InvalidOperationException("File size exceeds 50MB limit");
            }

            // Validate file type (signed documents)
            var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                throw new InvalidOperationException("Invalid file type. Only PDF, DOC, DOCX, JPG, and PNG files are supported");
            }

            // Generate unique public ID for the signed agreement (just the filename part)
            var fileId = Guid.NewGuid().ToString();

            // Upload to Cloudinary as raw file
            var uploadParams = new RawUploadParams()
            {
                File = new FileDescription(fileName, fileStream),
                PublicId = fileId,
                Folder = $"agreements/signed/{organizationId}/{consignorId}",
                Tags = $"agreement,signed,{organizationId},{consignorId}"
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
            {
                throw new InvalidOperationException($"Cloudinary upload failed: {uploadResult.Error.Message}");
            }

            var documentUrl = uploadResult.SecureUrl.ToString();
            _logger.LogInformation("Signed agreement uploaded successfully to Cloudinary: {DocumentUrl}", documentUrl);
            return documentUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload signed agreement for consignor {ConsignorId}", consignorId);
            throw;
        }
    }

    public async Task<bool> DeleteDocumentAsync(string documentUrl)
    {
        try
        {
            // Extract public ID from Cloudinary URL
            var publicId = ExtractPublicIdFromUrl(documentUrl);
            if (string.IsNullOrEmpty(publicId))
            {
                _logger.LogWarning("Could not extract public ID from URL: {DocumentUrl}", documentUrl);
                return false;
            }

            // Delete from Cloudinary
            var deleteParams = new DelResParams()
            {
                PublicIds = new List<string> { publicId },
                ResourceType = ResourceType.Raw
            };

            var result = await _cloudinary.DeleteResourcesAsync(deleteParams);

            if (result.Deleted?.ContainsKey(publicId) == true && result.Deleted[publicId] == "deleted")
            {
                _logger.LogInformation("Document deleted successfully from Cloudinary: {DocumentUrl}", documentUrl);
                return true;
            }
            else
            {
                _logger.LogWarning("Document deletion failed or not found: {DocumentUrl}", documentUrl);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {DocumentUrl}", documentUrl);
            return false;
        }
    }

    public async Task<bool> DocumentExistsAsync(string documentUrl)
    {
        try
        {
            _logger.LogInformation("[CLOUDINARY_DEBUG] Checking document existence for URL: {DocumentUrl}", documentUrl);

            // Extract public ID from Cloudinary URL
            var publicId = ExtractPublicIdFromUrl(documentUrl);
            _logger.LogInformation("[CLOUDINARY_DEBUG] Extracted public ID: {PublicId}", publicId);

            if (string.IsNullOrEmpty(publicId))
            {
                _logger.LogWarning("[CLOUDINARY_DEBUG] Public ID extraction failed for URL: {DocumentUrl}", documentUrl);
                return false;
            }

            // Check if resource exists in Cloudinary
            var getResourceParams = new GetResourceParams(publicId)
            {
                ResourceType = ResourceType.Raw
            };

            _logger.LogInformation("[CLOUDINARY_DEBUG] Calling Cloudinary GetResourceAsync with public ID: {PublicId}", publicId);
            var result = await _cloudinary.GetResourceAsync(getResourceParams);
            _logger.LogInformation("[CLOUDINARY_DEBUG] Cloudinary response status: {StatusCode}", result?.StatusCode);

            var exists = result?.StatusCode == System.Net.HttpStatusCode.OK;
            _logger.LogInformation("[CLOUDINARY_DEBUG] Final existence result: {Exists}", exists);

            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CLOUDINARY_DEBUG] Error checking document existence {DocumentUrl}", documentUrl);
            return false;
        }
    }

    public async Task<string> ExtractTextFromDocumentAsync(string documentUrl)
    {
        try
        {
            _logger.LogInformation("Extracting text from document: {DocumentUrl}", documentUrl);

            // Download the file from Cloudinary
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(documentUrl);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to download document from Cloudinary: {StatusCode}", response.StatusCode);
                return string.Empty;
            }

            // Get file extension from URL
            var extension = Path.GetExtension(new Uri(documentUrl).AbsolutePath).ToLowerInvariant();

            // Read content based on file type
            using var stream = await response.Content.ReadAsStreamAsync();

            switch (extension)
            {
                case ".txt":
                    using (var reader = new StreamReader(stream))
                    {
                        var content = await reader.ReadToEndAsync();
                        _logger.LogInformation("Successfully extracted text from TXT document");
                        return content;
                    }

                case ".pdf":
                    // For PDF files, return a placeholder for now
                    // TODO: Implement PDF text extraction using a PDF library like iTextSharp
                    _logger.LogInformation("PDF text extraction not yet implemented, returning placeholder");
                    return "PDF text extraction not yet implemented. Please use TXT format for now or implement PDF text extraction.";

                case ".doc":
                case ".docx":
                    // For Word documents, return a placeholder for now
                    // TODO: Implement Word document text extraction
                    _logger.LogInformation("Word document text extraction not yet implemented, returning placeholder");
                    return "Word document text extraction not yet implemented. Please use TXT format for now or implement Word document text extraction.";

                default:
                    _logger.LogWarning("Unsupported file type for text extraction: {Extension}", extension);
                    return $"Text extraction not supported for file type: {extension}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from document {DocumentUrl}", documentUrl);
            return string.Empty;
        }
    }

    /// <summary>
    /// Extract Cloudinary public ID from a Cloudinary URL
    /// Example: https://res.cloudinary.com/demo/raw/upload/v1234567890/agreements/templates/org-id/file-id.pdf
    /// Returns: agreements/templates/org-id/file-id
    /// </summary>
    private string? ExtractPublicIdFromUrl(string cloudinaryUrl)
    {
        try
        {
            var uri = new Uri(cloudinaryUrl);
            var pathParts = uri.AbsolutePath.Split('/');

            // Cloudinary URL structure: /[resource_type]/[delivery_type]/[version]/[public_id].[format]
            // Find the index of "upload" in the path
            var uploadIndex = Array.IndexOf(pathParts, "upload");
            if (uploadIndex == -1 || uploadIndex + 1 >= pathParts.Length)
            {
                return null;
            }

            // Get everything after upload, skip version if present
            var remainingParts = pathParts.Skip(uploadIndex + 1).ToList();

            // If first part looks like version (starts with 'v' followed by numbers), skip it
            if (remainingParts.Count > 0 && remainingParts[0].StartsWith("v") &&
                remainingParts[0].Length > 1 && remainingParts[0].Substring(1).All(char.IsDigit))
            {
                remainingParts = remainingParts.Skip(1).ToList();
            }

            if (remainingParts.Count == 0)
            {
                return null;
            }

            // Join remaining parts and remove file extension from last part
            var publicIdWithExtension = string.Join("/", remainingParts);
            var lastDotIndex = publicIdWithExtension.LastIndexOf('.');
            return lastDotIndex > 0 ? publicIdWithExtension.Substring(0, lastDotIndex) : publicIdWithExtension;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing Cloudinary URL: {CloudinaryUrl}", cloudinaryUrl);
            return null;
        }
    }
}