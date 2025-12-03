using System.ComponentModel.DataAnnotations;
using ConsignmentGenie.Core.Enums;

namespace ConsignmentGenie.Core.Entities
{
    public class Payout
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid OrganizationId { get; set; }
        public Organization Organization { get; set; } = null!;

        [Required]
        public Guid ConsignorId { get; set; }
        public Consignor Consignor { get; set; } = null!;

        // Payout Details
        [Required]
        [StringLength(50)]
        public string PayoutNumber { get; set; } = string.Empty;

        [Required]
        public DateTime PayoutDate { get; set; }

        [Required]
        [Range(0.01, 999999.99)]
        public decimal Amount { get; set; }

        [Required]
        public PayoutStatus Status { get; set; } = PayoutStatus.Paid;

        // Payment Info
        [Required]
        [StringLength(50)]
        public string PaymentMethod { get; set; } = string.Empty;

        [StringLength(100)]
        public string? PaymentReference { get; set; }

        // Period Covered
        [Required]
        public DateTime PeriodStart { get; set; }

        [Required]
        public DateTime PeriodEnd { get; set; }

        [Required]
        public int TransactionCount { get; set; }

        public string? Notes { get; set; }

        // QuickBooks Integration
        public bool SyncedToQuickBooks { get; set; } = false;
        [StringLength(100)]
        public string? QuickBooksBillId { get; set; }

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Guid? CreatedBy { get; set; }

        // Computed Properties for QuickBooks compatibility
        public decimal TotalAmount => Amount;
        public DateTime? PaidAt => PayoutDate;

        // Navigation Properties
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}