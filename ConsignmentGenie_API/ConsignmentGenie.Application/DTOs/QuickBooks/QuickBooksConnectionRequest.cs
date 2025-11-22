using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Application.DTOs.QuickBooks;

public class QuickBooksConnectionRequest
{
    [Required]
    public string Code { get; set; } = string.Empty;

    [Required]
    public string RealmId { get; set; } = string.Empty;

    [Required]
    public string State { get; set; } = string.Empty;
}

public class QuickBooksConnectionResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
}

public class QuickBooksSyncStatusResponse
{
    public bool IsConnected { get; set; }
    public string? CompanyName { get; set; }
    public DateTime? LastSyncDate { get; set; }
    public int PendingTransactions { get; set; }
    public int PendingPayouts { get; set; }
}