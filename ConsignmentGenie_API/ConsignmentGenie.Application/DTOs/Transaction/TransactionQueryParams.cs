namespace ConsignmentGenie.Application.DTOs.Transaction;

public class TransactionQueryParams
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public Guid? ProviderId { get; set; }
    public string? PaymentMethod { get; set; }
    // Source filtering removed for MVP - Phase 2+ feature
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SortBy { get; set; } = "SaleDate";
    public string? SortDirection { get; set; } = "desc";
}

public class MetricsQueryParams
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public Guid? ProviderId { get; set; }
}