namespace ConsignmentGenie.Application.DTOs.Transaction;

public class TransactionDto
{
    public Guid Id { get; set; }
    public DateTime TransactionDate { get; set; }

    // Legacy compatibility
    public DateTime SaleDate => TransactionDate;

    public string PaymentType { get; set; } = string.Empty;
    public string Source { get; set; } = "Manual";

    // Transaction totals
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal Total { get; set; }

    // Customer info
    public string? CustomerEmail { get; set; }
    public string? Notes { get; set; }

    // Multi-item data
    public List<TransactionItemDto> Items { get; set; } = new();

    // Audit
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class TransactionItemDto
{
    public Guid Id { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }

    // Split calculation
    public decimal ConsignorSplitPercentage { get; set; }
    public decimal ConsignorAmount { get; set; }
    public decimal StoreAmount { get; set; }

    // Item and Consignor info
    public ItemSummaryDto Item { get; set; } = new();
    public ConsignorSummaryDto Consignor { get; set; } = new();
}

public class ItemSummaryDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty; // Match Item.Title
    public string Sku { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; } // Current price
}

public class ConsignorSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
}