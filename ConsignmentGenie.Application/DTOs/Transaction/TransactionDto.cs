namespace ConsignmentGenie.Application.DTOs.Transaction;

public class TransactionDto
{
    public Guid Id { get; set; }
    public DateTime SaleDate { get; set; }
    public decimal SalePrice { get; set; }
    public decimal? SalesTaxAmount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    // Source removed for MVP - Phase 2+ feature

    // Commission split
    public decimal ConsignorSplitPercentage { get; set; }
    public decimal ConsignorAmount { get; set; }
    public decimal ShopAmount { get; set; }

    // Navigation data
    public ItemSummaryDto Item { get; set; } = new();
    public ProviderSummaryDto Consignor { get; set; } = new();
    public string? Notes { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ItemSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal OriginalPrice { get; set; }
}

public class ProviderSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
}