using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Application.DTOs.Transaction;

namespace ConsignmentGenie.Application.Services.Interfaces;

public interface ITransactionService
{
    Task<PagedResult<TransactionDto>> GetTransactionsAsync(Guid organizationId, TransactionQueryParams queryParams, CancellationToken cancellationToken = default);
    Task<TransactionDto?> GetTransactionByIdAsync(Guid organizationId, Guid transactionId, CancellationToken cancellationToken = default);
    Task<TransactionDto> CreateTransactionAsync(Guid organizationId, CreateTransactionRequest request, CancellationToken cancellationToken = default);
    Task<TransactionDto?> UpdateTransactionAsync(Guid organizationId, Guid transactionId, UpdateTransactionRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteTransactionAsync(Guid organizationId, Guid transactionId, CancellationToken cancellationToken = default);
    Task<SalesMetricsDto> GetSalesMetricsAsync(Guid organizationId, MetricsQueryParams queryParams, CancellationToken cancellationToken = default);
}