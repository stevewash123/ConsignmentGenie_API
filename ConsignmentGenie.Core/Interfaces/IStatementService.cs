using ConsignmentGenie.Core.DTOs.Statements;

namespace ConsignmentGenie.Core.Interfaces;

public interface IStatementService
{
    /// <summary>
    /// Generates a statement for a provider for a specific period
    /// </summary>
    Task<StatementDto> GenerateStatementAsync(Guid consignorId, DateOnly periodStart, DateOnly periodEnd);

    /// <summary>
    /// Generates statements for all providers for a specific month
    /// </summary>
    Task GenerateStatementsForMonthAsync(int year, int month);

    /// <summary>
    /// Gets all statements for a provider
    /// </summary>
    Task<List<StatementListDto>> GetStatementsAsync(Guid consignorId);

    /// <summary>
    /// Gets a specific statement by ID
    /// </summary>
    Task<StatementDto?> GetStatementAsync(Guid statementId, Guid consignorId);

    /// <summary>
    /// Gets a statement for a specific period
    /// </summary>
    Task<StatementDto?> GetStatementByPeriodAsync(Guid consignorId, DateOnly periodStart, DateOnly periodEnd);

    /// <summary>
    /// Marks a statement as viewed
    /// </summary>
    Task MarkAsViewedAsync(Guid statementId, Guid consignorId);

    /// <summary>
    /// Generates PDF for a statement
    /// </summary>
    Task<byte[]> GeneratePdfAsync(Guid statementId, Guid consignorId);

    /// <summary>
    /// Regenerates a statement (replaces existing)
    /// </summary>
    Task<StatementDto> RegenerateStatementAsync(Guid statementId, Guid consignorId);
}