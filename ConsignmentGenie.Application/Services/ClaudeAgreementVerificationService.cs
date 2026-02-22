using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace ConsignmentGenie.Application.Services;

public class ClaudeAgreementVerificationService : IAgreementVerificationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ClaudeAgreementVerificationService> _logger;
    private readonly ConsignmentGenieContext _context;
    private readonly string _apiKey;

    public ClaudeAgreementVerificationService(
        IConfiguration configuration,
        ILogger<ClaudeAgreementVerificationService> logger,
        ConsignmentGenieContext context)
    {
        _configuration = configuration;
        _logger = logger;
        _context = context;
        _apiKey = _configuration["Claude:ApiKey"] ?? "";

        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri("https://api.anthropic.com/");
        _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "ConsignmentGenie/1.0");
    }

    public async Task<AgreementVerificationResult> VerifyAgreementAsync(string fileUrl, Guid consignorId, Guid organizationId)
    {
        _logger.LogInformation("[AGREEMENT] Starting agreement verification for consignor {ConsignorId} in organization {OrganizationId}, file: {FileUrl}", consignorId, organizationId, fileUrl);

        try
        {
            // Validate input parameters
            if (string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogWarning("[AGREEMENT] Claude API key not configured, returning manual review recommendation");
                return new AgreementVerificationResult
                {
                    Recommendation = "manual_review",
                    Confidence = 0.0m,
                    Analysis = "Claude API key not configured. Manual review required.",
                    Provider = "claude",
                    Issues = new List<string> { "Service configuration incomplete" }
                };
            }

            // Get organization and consignor details for context
            var organization = await _context.Organizations.FirstOrDefaultAsync(o => o.Id == organizationId);
            var consignor = await _context.Consignors.FirstOrDefaultAsync(c => c.Id == consignorId && c.OrganizationId == organizationId);

            if (organization == null || consignor == null)
            {
                _logger.LogError("[AGREEMENT] Organization or consignor not found - OrgId: {OrganizationId}, ConsignorId: {ConsignorId}", organizationId, consignorId);
                throw new ArgumentException("Organization or consignor not found");
            }

            // Download and analyze the document
            var documentContent = await DownloadDocumentAsync(fileUrl);
            if (string.IsNullOrEmpty(documentContent))
            {
                return new AgreementVerificationResult
                {
                    Recommendation = "reject",
                    Confidence = 1.0m,
                    Analysis = "Unable to extract readable content from document",
                    Provider = "claude",
                    Issues = new List<string> { "Document content could not be extracted" }
                };
            }

            // TODO: Temporary stub - remove when Claude API is properly configured
            _logger.LogInformation("[AGREEMENT] Using stubbed AI response for development/testing");

            return new AgreementVerificationResult
            {
                Recommendation = "approve",
                Confidence = 0.85m,
                Analysis = "STUBBED RESPONSE: Document appears to be a properly signed consignment agreement with all required elements present.",
                Provider = "claude-stubbed",
                Issues = new List<string>(),
                PositiveFindings = new List<string>
                {
                    "Document contains required signatures",
                    "Agreement terms are clearly defined",
                    "Parties are correctly identified"
                },
                RawResult = @"{""recommendation"": ""approve"", ""confidence"": 0.85, ""analysis"": ""STUBBED RESPONSE""}"
            };

            // ORIGINAL CODE (temporarily commented out):
            // Prepare Claude analysis prompt
            // var prompt = BuildAnalysisPrompt(documentContent, organization, consignor);
            // Call Claude API
            // var claudeResponse = await CallClaudeApiAsync(prompt);
            // Parse response into structured result
            // return ParseClaudeResponse(claudeResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AGREEMENT] Error during agreement verification for consignor {ConsignorId}", consignorId);
            return new AgreementVerificationResult
            {
                Recommendation = "manual_review",
                Confidence = 0.0m,
                Analysis = $"Error during analysis: {ex.Message}",
                Provider = "claude",
                Issues = new List<string> { "Analysis error - manual review required" }
            };
        }
    }

    public async Task<AgreementVerificationResult> ReVerifyAgreementAsync(Guid uploadId, Guid organizationId)
    {
        _logger.LogInformation("[AGREEMENT] Starting re-verification for upload {UploadId} in organization {OrganizationId}", uploadId, organizationId);

        var upload = await _context.ConsignorAgreementUploads
            .Include(u => u.Consignor)
            .FirstOrDefaultAsync(u => u.Id == uploadId && u.Consignor.OrganizationId == organizationId);

        if (upload == null)
        {
            _logger.LogError("[AGREEMENT] Upload not found - UploadId: {UploadId}, OrgId: {OrganizationId}", uploadId, organizationId);
            throw new ArgumentException("Upload not found");
        }

        return await VerifyAgreementAsync(upload.FileUrl, upload.ConsignorId, organizationId);
    }

    public async Task<DocumentValidationResult> ValidateDocumentAsync(string fileUrl, string fileMimeType, int fileSizeBytes)
    {
        _logger.LogInformation("[AGREEMENT] Validating document: {FileUrl}, MIME: {MimeType}, Size: {Size} bytes", fileUrl, fileMimeType, fileSizeBytes);

        var result = new DocumentValidationResult();

        // Check file size (max 10MB)
        const int maxSizeBytes = 10 * 1024 * 1024;
        if (fileSizeBytes > maxSizeBytes)
        {
            result.Errors.Add($"File size ({fileSizeBytes:N0} bytes) exceeds maximum allowed size ({maxSizeBytes:N0} bytes)");
        }

        // Check MIME type
        var allowedMimeTypes = new[]
        {
            "application/pdf",
            "image/jpeg",
            "image/jpg",
            "image/png",
            "image/gif",
            "text/plain",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        };

        if (!allowedMimeTypes.Contains(fileMimeType?.ToLowerInvariant()))
        {
            result.Errors.Add($"File type '{fileMimeType}' is not supported. Allowed types: PDF, images (JPG, PNG, GIF), text, and Word documents");
        }

        // Check if URL is accessible
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, fileUrl);
            using var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                result.Errors.Add($"File is not accessible at the provided URL (HTTP {response.StatusCode})");
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Unable to access file URL: {ex.Message}");
        }

        result.IsValid = result.Errors.Count == 0;

        if (!result.IsValid)
        {
            _logger.LogWarning("[AGREEMENT] Document validation failed for {FileUrl}: {Errors}", fileUrl, string.Join("; ", result.Errors));
        }

        return result;
    }

    private async Task<string> DownloadDocumentAsync(string fileUrl)
    {
        _logger.LogDebug("[AGREEMENT] Downloading document from {FileUrl}", fileUrl);

        try
        {
            using var response = await _httpClient.GetAsync(fileUrl);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            // For now, return content as-is. In a full implementation, you would:
            // 1. Check content type and handle PDFs/images appropriately
            // 2. Use OCR for images
            // 3. Extract text from PDFs
            // 4. Handle various document formats

            _logger.LogDebug("[AGREEMENT] Downloaded document content, length: {Length} characters", content.Length);
            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AGREEMENT] Failed to download document from {FileUrl}", fileUrl);
            return string.Empty;
        }
    }

    private string BuildAnalysisPrompt(string documentContent, Organization organization, Consignor consignor)
    {
        var prompt = $@"
You are an expert document analyst specializing in consignment agreements. Please analyze the following document and determine if it represents a valid, signed consignment agreement.

**Context:**
- Shop: {organization.Name}
- Consignor: {consignor.Name} {consignor}
- Expected Commission Rate: {consignor.CommissionRate:P}

**Document Content:**
{documentContent}

**Analysis Requirements:**
Please analyze this document and provide your assessment in the following JSON format:

{{
  ""recommendation"": ""approve"" | ""reject"" | ""manual_review"",
  ""confidence"": 0.95,
  ""analysis"": ""Detailed explanation of your findings"",
  ""issues"": [""List of any problems found""],
  ""positiveFindings"": [""List of positive elements identified""]
}}

**Evaluation Criteria:**
1. **Signature Verification**: Does the document appear to be signed by the consignor?
2. **Agreement Terms**: Does it contain standard consignment agreement terms?
3. **Correct Parties**: Are the correct parties (shop and consignor) named?
4. **Commission Terms**: Are commission/split terms clearly defined?
5. **Legibility**: Is the document clear and readable?
6. **Completeness**: Does it appear to be a complete agreement?

**Recommendation Guidelines:**
- ""approve"": Clear, properly signed consignment agreement with all required elements
- ""reject"": Obviously fake, incomplete, or incorrect document
- ""manual_review"": Uncertain, partial, or requires human judgment

Provide your analysis as valid JSON only, no other text.
";

        return prompt.Trim();
    }

    private async Task<string> CallClaudeApiAsync(string prompt)
    {
        _logger.LogDebug("[AGREEMENT] Calling Claude API for document analysis");

        var requestData = new
        {
            model = "claude-3-haiku-20240307",
            max_tokens = 1000,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = prompt
                }
            }
        };

        var json = JsonSerializer.Serialize(requestData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync("v1/messages", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);

            if (responseData.TryGetProperty("content", out var contentArray) &&
                contentArray.GetArrayLength() > 0 &&
                contentArray[0].TryGetProperty("text", out var textElement))
            {
                var claudeResponse = textElement.GetString() ?? "";
                _logger.LogDebug("[AGREEMENT] Claude API response received, length: {Length} characters", claudeResponse.Length);
                return claudeResponse;
            }

            _logger.LogWarning("[AGREEMENT] Unexpected Claude API response format: {Response}", responseContent);
            return "";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AGREEMENT] Claude API call failed");
            throw;
        }
    }

    private AgreementVerificationResult ParseClaudeResponse(string claudeResponse)
    {
        _logger.LogDebug("[AGREEMENT] Parsing Claude response");

        try
        {
            // Extract JSON from response (in case there's extra text)
            var startIndex = claudeResponse.IndexOf('{');
            var endIndex = claudeResponse.LastIndexOf('}');

            if (startIndex >= 0 && endIndex >= startIndex)
            {
                var jsonResponse = claudeResponse.Substring(startIndex, endIndex - startIndex + 1);
                var data = JsonSerializer.Deserialize<JsonElement>(jsonResponse);

                var result = new AgreementVerificationResult
                {
                    Provider = "claude",
                    RawResult = claudeResponse
                };

                if (data.TryGetProperty("recommendation", out var recElement))
                    result.Recommendation = recElement.GetString() ?? "manual_review";

                if (data.TryGetProperty("confidence", out var confElement))
                    result.Confidence = (decimal)confElement.GetDouble();

                if (data.TryGetProperty("analysis", out var analysisElement))
                    result.Analysis = analysisElement.GetString() ?? "";

                if (data.TryGetProperty("issues", out var issuesElement))
                {
                    result.Issues = issuesElement.EnumerateArray()
                        .Select(item => item.GetString() ?? "")
                        .Where(item => !string.IsNullOrEmpty(item))
                        .ToList();
                }

                if (data.TryGetProperty("positiveFindings", out var positivesElement))
                {
                    result.PositiveFindings = positivesElement.EnumerateArray()
                        .Select(item => item.GetString() ?? "")
                        .Where(item => !string.IsNullOrEmpty(item))
                        .ToList();
                }

                _logger.LogInformation("[AGREEMENT] Successfully parsed Claude response - Recommendation: {Recommendation}, Confidence: {Confidence}",
                    result.Recommendation, result.Confidence);

                return result;
            }

            _logger.LogWarning("[AGREEMENT] Could not extract JSON from Claude response");
            return new AgreementVerificationResult
            {
                Recommendation = "manual_review",
                Confidence = 0.0m,
                Analysis = "Could not parse Claude response",
                Provider = "claude",
                RawResult = claudeResponse,
                Issues = new List<string> { "Response parsing failed" }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AGREEMENT] Error parsing Claude response: {Response}", claudeResponse);
            return new AgreementVerificationResult
            {
                Recommendation = "manual_review",
                Confidence = 0.0m,
                Analysis = $"Error parsing response: {ex.Message}",
                Provider = "claude",
                RawResult = claudeResponse,
                Issues = new List<string> { "Response parsing error" }
            };
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _httpClient?.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}