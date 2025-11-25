using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Application.DTOs;

// Query Parameters
public class ProviderQueryParams
{
    public string? Search { get; set; }              // Search name, email, number
    public string? Status { get; set; }              // Active, Deactivated, Pending, or null for all
    public bool? HasPendingBalance { get; set; }     // Filter to providers with unpaid earnings
    public string? SortBy { get; set; } = "Name";    // Name, CreatedAt, ItemCount, Balance
    public string? SortDirection { get; set; } = "asc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

// List View DTO
public class ProviderListDto
{
    public Guid ProviderId { get; set; }
    public string ProviderNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public decimal CommissionRate { get; set; }
    public string Status { get; set; } = string.Empty;

    // Summary Stats
    public int ActiveItemCount { get; set; }
    public int TotalItemCount { get; set; }
    public decimal PendingBalance { get; set; }
    public decimal TotalEarnings { get; set; }

    // Flags
    public bool HasPortalAccess { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Summary View DTO (for approvals and simple lists)
public class ProviderApprovalSummaryDto
{
    public Guid ProviderId { get; set; }
    public string ProviderNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public decimal CommissionRate { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

// Approval History DTO
public class ProviderApprovalHistoryDto
{
    public Guid ProviderId { get; set; }
    public string ProviderNumber { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? ApprovalStatus { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedByName { get; set; }
    public string? RejectedReason { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Detail View DTO
public class ProviderDetailDto
{
    public Guid ProviderId { get; set; }
    public Guid? UserId { get; set; }
    public string ProviderNumber { get; set; } = string.Empty;

    // Contact Info
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }

    // Address
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? FullAddress { get; set; }         // Formatted address

    // Business Terms
    public decimal CommissionRate { get; set; }
    public DateTime? ContractStartDate { get; set; }
    public DateTime? ContractEndDate { get; set; }
    public bool IsContractExpired { get; set; }

    // Payment
    public string? PreferredPaymentMethod { get; set; }
    public string? PaymentDetails { get; set; }

    // Status
    public string Status { get; set; } = string.Empty;
    public DateTime? StatusChangedAt { get; set; }
    public string? StatusChangedReason { get; set; }

    // If pending approval
    public string? ApprovalStatus { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedByName { get; set; }
    public string? RejectedReason { get; set; }

    // Notes
    public string? Notes { get; set; }

    // Portal Access
    public bool HasPortalAccess { get; set; }
    public DateTime? LastPortalLogin { get; set; }

    // Stats
    public ProviderMetricsDto? Metrics { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// Metrics DTO
public class ProviderMetricsDto
{
    // Items
    public int TotalItems { get; set; }
    public int AvailableItems { get; set; }
    public int SoldItems { get; set; }
    public int RemovedItems { get; set; }
    public decimal InventoryValue { get; set; }     // Sum of available item prices

    // Earnings
    public decimal PendingBalance { get; set; }     // Unpaid earnings
    public decimal TotalEarnings { get; set; }      // All-time earnings
    public decimal TotalPaid { get; set; }          // All-time payouts
    public decimal EarningsThisMonth { get; set; }
    public decimal EarningsLastMonth { get; set; }

    // Activity
    public int SalesThisMonth { get; set; }
    public int SalesLastMonth { get; set; }
    public DateTime? LastSaleDate { get; set; }
    public DateTime? LastPayoutDate { get; set; }
    public decimal LastPayoutAmount { get; set; }

    // Averages
    public decimal AverageItemPrice { get; set; }
    public decimal AverageDaysToSell { get; set; }
}

// Dashboard Metrics DTO
public class ProviderDashboardMetricsDto
{
    public int TotalProviders { get; set; }
    public int ActiveProviders { get; set; }
    public int PendingProviders { get; set; }
    public int DeactivatedProviders { get; set; }
    public int NewProvidersThisMonth { get; set; }
    public decimal ProviderGrowthRate { get; set; }
    public List<ProviderTopPerformerDto> TopProvidersByBalance { get; set; } = new();
}

// Top Performer DTO
public class ProviderTopPerformerDto
{
    public Guid ProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public decimal PendingBalance { get; set; }
    public decimal TotalEarnings { get; set; }
    public int ActiveItems { get; set; }
    public int TotalItems { get; set; }
}

// Activity DTO
public class ProviderActivityDto
{
    public Guid ProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public int DaysRange { get; set; }
    public List<ProviderActivityTransactionDto> RecentTransactions { get; set; } = new();
    public List<ProviderActivityItemDto> RecentItems { get; set; } = new();
    public List<ProviderActivityPayoutDto> RecentPayouts { get; set; } = new();
    public int TotalTransactions { get; set; }
    public int TotalItemsAdded { get; set; }
    public int TotalPayouts { get; set; }
}

// Activity Transaction DTO
public class ProviderActivityTransactionDto
{
    public Guid TransactionId { get; set; }
    public DateTime SaleDate { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public decimal SalePrice { get; set; }
    public decimal ProviderAmount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
}

// Activity Item DTO
public class ProviderActivityItemDto
{
    public Guid ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

// Activity Payout DTO
public class ProviderActivityPayoutDto
{
    public Guid PayoutId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PayoutDate { get; set; }
    public string Method { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

// Create Request
public class CreateProviderRequest
{
    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [EmailAddress, MaxLength(255)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    // Address (optional)
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }

    // Business Terms
    [Required, Range(0, 1)]
    public decimal CommissionRate { get; set; }

    public DateTime? ContractStartDate { get; set; }
    public DateTime? ContractEndDate { get; set; }

    // Payment
    public string? PreferredPaymentMethod { get; set; }
    public string? PaymentDetails { get; set; }

    // Notes
    public string? Notes { get; set; }

    // Options
    public bool SendInviteEmail { get; set; } = false;
}

// Update Request
public class UpdateProviderRequest
{
    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [EmailAddress, MaxLength(255)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    // Address
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }

    // Business Terms
    [Required, Range(0, 1)]
    public decimal CommissionRate { get; set; }

    public DateTime? ContractStartDate { get; set; }
    public DateTime? ContractEndDate { get; set; }

    // Payment
    public string? PreferredPaymentMethod { get; set; }
    public string? PaymentDetails { get; set; }

    // Notes
    public string? Notes { get; set; }
}

// Status Change Requests
public class DeactivateProviderRequest
{
    [MaxLength(255)]
    public string? Reason { get; set; }
}

public class RejectProviderRequest
{
    [Required, MaxLength(255)]
    public string Reason { get; set; } = string.Empty;
}

// Store Code DTOs
public class StoreCodeDto
{
    public string StoreCode { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public string RegistrationUrl { get; set; } = string.Empty;
}

public class ToggleStoreCodeRequest
{
    public bool IsEnabled { get; set; }
}

// Settings Summary DTO
public class ProviderSettingsSummaryDto
{
    public bool AllowSelfRegistration { get; set; }
    public string RegistrationCode { get; set; } = string.Empty;
    public string RegistrationUrl { get; set; } = string.Empty;
    public int TotalProviders { get; set; }
    public int PendingRegistrations { get; set; }
    public decimal DefaultCommissionRate { get; set; }
}

// Payment Methods Lookup
public static class PaymentMethods
{
    public static readonly List<string> All = new()
    {
        "Cash",
        "Check",
        "Venmo",
        "Zelle",
        "PayPal",
        "Direct Deposit",
        "Store Credit",
        "Other"
    };
}