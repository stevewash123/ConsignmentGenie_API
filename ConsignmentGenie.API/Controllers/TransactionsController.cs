using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Application.DTOs.Transaction;
using ConsignmentGenie.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;
    private readonly ILogger<TransactionsController> _logger;

    public TransactionsController(
        ITransactionService transactionService,
        ILogger<TransactionsController> logger)
    {
        _transactionService = transactionService;
        _logger = logger;
    }

    private Guid GetOrganizationId()
    {
        var orgIdClaim = User.FindFirst("OrganizationId")?.Value;
        if (string.IsNullOrEmpty(orgIdClaim) || !Guid.TryParse(orgIdClaim, out var orgId))
        {
            throw new UnauthorizedAccessException("Organization ID not found in token");
        }
        return orgId;
    }

    /// <summary>
    /// Get transactions with filtering and pagination
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<TransactionDto>>> GetTransactions(
        [FromQuery] TransactionQueryParams queryParams)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var result = await _transactionService.GetTransactionsAsync(organizationId, queryParams);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transactions");
            return StatusCode(500, "An error occurred while retrieving transactions");
        }
    }

    /// <summary>
    /// Get transaction by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<TransactionDto>> GetTransaction(Guid id)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var transaction = await _transactionService.GetTransactionByIdAsync(organizationId, id);

            if (transaction == null)
                return NotFound("Transaction not found");

            return Ok(transaction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transaction {TransactionId}", id);
            return StatusCode(500, "An error occurred while retrieving the transaction");
        }
    }

    /// <summary>
    /// Process a new sale
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<TransactionDto>> CreateTransaction(
        [FromBody] CreateTransactionRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var organizationId = GetOrganizationId();
            var transaction = await _transactionService.CreateTransactionAsync(organizationId, request);

            return CreatedAtAction(
                nameof(GetTransaction),
                new { id = transaction.Id },
                transaction);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation creating transaction");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating transaction");
            return StatusCode(500, "An error occurred while creating the transaction");
        }
    }

    /// <summary>
    /// Edit transaction (corrections, notes, etc)
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<TransactionDto>> UpdateTransaction(
        Guid id,
        [FromBody] UpdateTransactionRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var organizationId = GetOrganizationId();
            var transaction = await _transactionService.UpdateTransactionAsync(organizationId, id, request);

            if (transaction == null)
                return NotFound("Transaction not found");

            return Ok(transaction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating transaction {TransactionId}", id);
            return StatusCode(500, "An error occurred while updating the transaction");
        }
    }

    /// <summary>
    /// Void/delete transaction
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteTransaction(Guid id)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var success = await _transactionService.DeleteTransactionAsync(organizationId, id);

            if (!success)
                return NotFound("Transaction not found");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting transaction {TransactionId}", id);
            return StatusCode(500, "An error occurred while deleting the transaction");
        }
    }

    /// <summary>
    /// Get sales metrics for dashboard
    /// </summary>
    [HttpGet("metrics")]
    public async Task<ActionResult<SalesMetricsDto>> GetSalesMetrics(
        [FromQuery] MetricsQueryParams queryParams)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var metrics = await _transactionService.GetSalesMetricsAsync(organizationId, queryParams);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sales metrics");
            return StatusCode(500, "An error occurred while retrieving sales metrics");
        }
    }
}