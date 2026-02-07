using ConsignmentGenie.Core.Entities;

namespace ConsignmentGenie.Application.Services.Interfaces;

public interface IAgreementVerificationService
{
    /// <summary>
    /// Verifies an uploaded agreement document using AI analysis
    /// </summary>
    /// <param name="fileUrl">URL of the uploaded agreement document</param>
    /// <param name="consignorId">ID of the consignor this agreement is for</param>
    /// <param name="organizationId">ID of the organization for multi-tenant scoping</param>
    /// <returns>Verification result with recommendation and details</returns>
    Task<AgreementVerificationResult> VerifyAgreementAsync(string fileUrl, Guid consignorId, Guid organizationId);

    /// <summary>
    /// Re-analyzes an existing agreement upload with updated criteria
    /// </summary>
    /// <param name="uploadId">ID of the ConsignorAgreementUpload to re-analyze</param>
    /// <param name="organizationId">ID of the organization for multi-tenant scoping</param>
    /// <returns>Updated verification result</returns>
    Task<AgreementVerificationResult> ReVerifyAgreementAsync(Guid uploadId, Guid organizationId);

    /// <summary>
    /// Validates that an agreement document meets basic requirements
    /// </summary>
    /// <param name="fileUrl">URL of the document to validate</param>
    /// <param name="fileMimeType">MIME type of the document</param>
    /// <param name="fileSizeBytes">Size of the document in bytes</param>
    /// <returns>Validation result indicating if document is acceptable for processing</returns>
    Task<DocumentValidationResult> ValidateDocumentAsync(string fileUrl, string fileMimeType, int fileSizeBytes);
}

public class AgreementVerificationResult
{
    /// <summary>
    /// Recommendation based on AI analysis: "approve", "reject", "manual_review"
    /// </summary>
    public string Recommendation { get; set; } = string.Empty;

    /// <summary>
    /// Confidence score from 0.0 to 1.0
    /// </summary>
    public decimal Confidence { get; set; }

    /// <summary>
    /// Detailed explanation of the analysis
    /// </summary>
    public string Analysis { get; set; } = string.Empty;

    /// <summary>
    /// Raw verification data as JSON for audit trail
    /// </summary>
    public string RawResult { get; set; } = string.Empty;

    /// <summary>
    /// Provider that performed the verification (e.g., "claude", "manual")
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// List of issues found during verification
    /// </summary>
    public List<string> Issues { get; set; } = new();

    /// <summary>
    /// List of positive findings during verification
    /// </summary>
    public List<string> PositiveFindings { get; set; } = new();
}

public class DocumentValidationResult
{
    /// <summary>
    /// Whether the document passes basic validation
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// List of validation errors if any
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Warnings that don't prevent processing but should be noted
    /// </summary>
    public List<string> Warnings { get; set; } = new();
}