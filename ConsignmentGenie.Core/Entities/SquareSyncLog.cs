using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.Entities;

public class SquareSyncLog : BaseEntity
{
    public Guid OrganizationId { get; set; }

    public Guid SquareConnectionId { get; set; }

    public DateTime SyncStarted { get; set; }

    public DateTime? SyncCompleted { get; set; }

    public bool Success { get; set; }

    public int TransactionsImported { get; set; }

    public int TransactionsMatched { get; set; }

    public int TransactionsUnmatched { get; set; }

    public string? ErrorMessage { get; set; }

    public string? Details { get; set; }  // JSON: array of {paymentId, status, matchedItemId?}

    // Navigation properties
    public SquareConnection SquareConnection { get; set; } = null!;
}