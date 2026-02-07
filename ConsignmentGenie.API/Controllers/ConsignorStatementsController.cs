using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Core.DTOs.Statements;
using ConsignmentGenie.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/consignor/statements")]
[Authorize]
public class ConsignorStatementsController : ControllerBase
{
    private readonly IStatementService _statementService;
    private readonly ILogger<ConsignorStatementsController> _logger;

    public ConsignorStatementsController(
        IStatementService statementService,
        ILogger<ConsignorStatementsController> logger)
    {
        _statementService = statementService;
        _logger = logger;
    }

    /// <summary>
    /// Get all statements for the authenticated provider
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<StatementListDto>>>> GetStatements()
    {
        try
        {
            var consignorId = GetCurrentConsignorId();
            if (consignorId == null)
                return Unauthorized(ApiResponse<List<StatementListDto>>.ErrorResult("Consignor context required"));

            var statements = await _statementService.GetStatementsAsync(consignorId.Value);
            return Ok(ApiResponse<List<StatementListDto>>.SuccessResult(statements));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statements");
            return StatusCode(500, ApiResponse<List<StatementListDto>>.ErrorResult("An error occurred while retrieving statements"));
        }
    }

    /// <summary>
    /// Get a specific statement by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<StatementDto>>> GetStatement(Guid id)
    {
        try
        {
            var consignorId = GetCurrentConsignorId();
            if (consignorId == null)
                return Unauthorized(ApiResponse<StatementDto>.ErrorResult("Consignor context required"));

            var statement = await _statementService.GetStatementAsync(id, consignorId.Value);
            if (statement == null)
                return NotFound(ApiResponse<StatementDto>.ErrorResult("Statement not found"));

            // Mark as viewed
            await _statementService.MarkAsViewedAsync(id, consignorId.Value);

            return Ok(ApiResponse<StatementDto>.SuccessResult(statement));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statement {StatementId}", id);
            return StatusCode(500, ApiResponse<StatementDto>.ErrorResult("An error occurred while retrieving statement"));
        }
    }

    /// <summary>
    /// Get statement for a specific period
    /// </summary>
    [HttpGet("{year}/{month}")]
    public async Task<ActionResult<ApiResponse<StatementDto>>> GetStatementByPeriod(int year, int month)
    {
        try
        {
            var consignorId = GetCurrentConsignorId();
            if (consignorId == null)
                return Unauthorized(ApiResponse<StatementDto>.ErrorResult("Consignor context required"));

            if (month < 1 || month > 12)
                return BadRequest(ApiResponse<StatementDto>.ErrorResult("Invalid month"));

            var periodStart = new DateOnly(year, month, 1);
            var periodEnd = periodStart.AddMonths(1).AddDays(-1);

            var statement = await _statementService.GetStatementByPeriodAsync(consignorId.Value, periodStart, periodEnd);
            if (statement == null)
                return NotFound(ApiResponse<StatementDto>.ErrorResult("Statement not found"));

            // Mark as viewed
            await _statementService.MarkAsViewedAsync(statement.Id, consignorId.Value);

            return Ok(ApiResponse<StatementDto>.SuccessResult(statement));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statement for {Year}-{Month}", year, month);
            return StatusCode(500, ApiResponse<StatementDto>.ErrorResult("An error occurred while retrieving statement"));
        }
    }

    /// <summary>
    /// Download PDF for a statement
    /// </summary>
    [HttpGet("{id}/pdf")]
    public async Task<IActionResult> GetStatementPdf(Guid id)
    {
        try
        {
            var consignorId = GetCurrentConsignorId();
            if (consignorId == null)
                return Unauthorized("Consignor context required");

            var statement = await _statementService.GetStatementAsync(id, consignorId.Value);
            if (statement == null)
                return NotFound();

            var pdfBytes = await _statementService.GeneratePdfAsync(id, consignorId.Value);
            var fileName = $"{statement.StatementNumber}.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF for statement {StatementId}", id);
            return StatusCode(500, "An error occurred while generating PDF");
        }
    }

    /// <summary>
    /// Download PDF for statement by period
    /// </summary>
    [HttpGet("{year}/{month}/pdf")]
    public async Task<IActionResult> GetStatementPdfByPeriod(int year, int month)
    {
        try
        {
            var consignorId = GetCurrentConsignorId();
            if (consignorId == null)
                return Unauthorized("Consignor context required");

            if (month < 1 || month > 12)
                return BadRequest("Invalid month");

            var periodStart = new DateOnly(year, month, 1);
            var periodEnd = periodStart.AddMonths(1).AddDays(-1);

            var statement = await _statementService.GetStatementByPeriodAsync(consignorId.Value, periodStart, periodEnd);
            if (statement == null)
                return NotFound();

            var pdfBytes = await _statementService.GeneratePdfAsync(statement.Id, consignorId.Value);
            var fileName = $"{statement.StatementNumber}.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF for statement {Year}-{Month}", year, month);
            return StatusCode(500, "An error occurred while generating PDF");
        }
    }

    /// <summary>
    /// Regenerate a statement (for admin/debugging purposes)
    /// </summary>
    [HttpPost("{id}/regenerate")]
    public async Task<ActionResult<ApiResponse<StatementDto>>> RegenerateStatement(Guid id)
    {
        try
        {
            var consignorId = GetCurrentConsignorId();
            if (consignorId == null)
                return Unauthorized(ApiResponse<StatementDto>.ErrorResult("Consignor context required"));

            var statement = await _statementService.RegenerateStatementAsync(id, consignorId.Value);
            return Ok(ApiResponse<StatementDto>.SuccessResult(statement));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error regenerating statement {StatementId}", id);
            return StatusCode(500, ApiResponse<StatementDto>.ErrorResult("An error occurred while regenerating statement"));
        }
    }

    private Guid? GetCurrentConsignorId()
    {
        // This assumes the provider ID is available in claims
        // You may need to adjust based on how authentication is implemented
        var consignorIdClaim = User.FindFirst("ConsignorId")?.Value;
        if (Guid.TryParse(consignorIdClaim, out var consignorId))
            return consignorId;

        // Fallback: look up provider by user ID
        // This would require injecting the context or a service to do the lookup
        // For now, return null and let the calling method handle it
        return null;
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}