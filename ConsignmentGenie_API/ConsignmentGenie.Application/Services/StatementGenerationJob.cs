using ConsignmentGenie.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace ConsignmentGenie.Application.Services;

/// <summary>
/// Background job service for generating monthly statements
/// </summary>
public class StatementGenerationJob
{
    private readonly IStatementService _statementService;
    private readonly ILogger<StatementGenerationJob> _logger;

    public StatementGenerationJob(
        IStatementService statementService,
        ILogger<StatementGenerationJob> logger)
    {
        _statementService = statementService;
        _logger = logger;
    }

    /// <summary>
    /// Generates statements for all providers for the previous month
    /// Designed to be called on the 1st of each month
    /// </summary>
    public async Task GenerateMonthlyStatementsAsync()
    {
        try
        {
            var previousMonth = DateTime.UtcNow.AddMonths(-1);
            var year = previousMonth.Year;
            var month = previousMonth.Month;

            _logger.LogInformation("Starting monthly statement generation for {Year}-{Month}", year, month);

            await _statementService.GenerateStatementsForMonthAsync(year, month);

            _logger.LogInformation("Completed monthly statement generation for {Year}-{Month}", year, month);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate monthly statements");
            throw; // Re-throw to let the background job framework handle retry logic
        }
    }

    /// <summary>
    /// Generates statements for a specific month (for admin/testing purposes)
    /// </summary>
    public async Task GenerateStatementsForSpecificMonthAsync(int year, int month)
    {
        try
        {
            if (month < 1 || month > 12)
                throw new ArgumentException("Invalid month", nameof(month));

            _logger.LogInformation("Starting statement generation for {Year}-{Month}", year, month);

            await _statementService.GenerateStatementsForMonthAsync(year, month);

            _logger.LogInformation("Completed statement generation for {Year}-{Month}", year, month);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate statements for {Year}-{Month}", year, month);
            throw;
        }
    }
}