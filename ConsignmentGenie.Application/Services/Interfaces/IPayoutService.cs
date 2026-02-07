using ConsignmentGenie.Application.DTOs.Payout;

namespace ConsignmentGenie.Application.Services.Interfaces;

/// <summary>
/// Payout service abstraction for consignor payments
/// MVP: ManualPayoutService (tracking only, owner pays manually)
/// Phase 5+: PayPalPayoutService, StripeConnectService, etc.
/// </summary>
public interface IPayoutService
{
    /// <summary>
    /// Generate payout data for a consignor for a date range
    /// </summary>
    Task<PayoutReportDto> GeneratePayoutAsync(Guid consignorId, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Generate payout data for all consignors for a date range
    /// </summary>
    Task<List<PayoutReportDto>> GenerateAllPayoutsAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Mark a payout as paid (MVP: manual tracking only)
    /// </summary>
    Task MarkPayoutAsPaidAsync(Guid payoutId, string paymentMethod, string? notes = null);

    /// <summary>
    /// Export payout data to CSV for accounting software import
    /// </summary>
    Task<byte[]> ExportPayoutToCsvAsync(Guid payoutId);

    /// <summary>
    /// Export multiple payouts to CSV
    /// </summary>
    Task<byte[]> ExportPayoutsToCsvAsync(List<Guid> payoutIds);

    /// <summary>
    /// Calculate pending payout amount for a consignor
    /// </summary>
    Task<decimal> GetPendingPayoutAmountAsync(Guid consignorId);

    /// <summary>
    /// Get all pending payouts for the organization
    /// </summary>
    Task<List<PayoutSummaryDto>> GetPendingPayoutsAsync();

    /// <summary>
    /// Process automated payout (Phase 5+ feature - will throw NotImplementedException in MVP)
    /// </summary>
    Task<PayoutResultDto> ProcessAutomatedPayoutAsync(Guid payoutId);
}

/// <summary>
/// Result of automated payout processing
/// </summary>
public class PayoutResultDto
{
    public bool Success { get; set; }
    public string? TransactionId { get; set; }
    public string? ErrorMessage { get; set; }
    public decimal AmountPaid { get; set; }
    public DateTime ProcessedAt { get; set; }
}