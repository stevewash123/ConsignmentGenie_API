namespace ConsignmentGenie.Application.Services.Interfaces;

/// <summary>
/// Service for managing document uploads and storage (agreements, PDFs, etc.)
/// </summary>
public interface IDocumentService
{
    /// <summary>
    /// Upload an agreement template file for an organization
    /// </summary>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="fileStream">File stream</param>
    /// <param name="fileName">Original file name</param>
    /// <returns>Cloudinary URL of the uploaded file</returns>
    Task<string> UploadAgreementTemplateAsync(Guid organizationId, Stream fileStream, string fileName);

    /// <summary>
    /// Upload a signed agreement file for a consignor
    /// </summary>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="consignorId">Consignor ID</param>
    /// <param name="fileStream">File stream</param>
    /// <param name="fileName">Original file name</param>
    /// <returns>Cloudinary URL of the uploaded file</returns>
    Task<string> UploadSignedAgreementAsync(Guid organizationId, Guid consignorId, Stream fileStream, string fileName);

    /// <summary>
    /// Delete a document by its Cloudinary URL
    /// </summary>
    /// <param name="documentUrl">Cloudinary URL of the document</param>
    /// <returns>True if successful</returns>
    Task<bool> DeleteDocumentAsync(string documentUrl);

    /// <summary>
    /// Check if a document exists by its Cloudinary URL
    /// </summary>
    /// <param name="documentUrl">Cloudinary URL of the document</param>
    /// <returns>True if the document exists</returns>
    Task<bool> DocumentExistsAsync(string documentUrl);

    /// <summary>
    /// Extract text content from a document stored in Cloudinary
    /// </summary>
    /// <param name="documentUrl">Cloudinary URL of the document</param>
    /// <returns>Text content of the document</returns>
    Task<string> ExtractTextFromDocumentAsync(string documentUrl);
}