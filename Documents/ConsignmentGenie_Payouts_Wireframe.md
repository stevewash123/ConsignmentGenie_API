# ConsignmentGenie - Payouts Workflow Wireframe & Data Requirements

## Document Purpose
Guide CC development of the Payouts vertical for the Owner UI.

---

## Payouts Overview

**What it does:** Tracks money owed to providers from sold items, generates payout reports, and records when providers are paid.

**User Story:** As a shop owner, I need to see what I owe each provider, generate a report of their sales, pay them (outside the system via Venmo/Check/etc.), and mark the payout as complete.

---

## Payout Lifecycle

```
Item Sold (Transaction created)
    ↓
Provider's balance increases (calculated, not stored)
    ↓
Owner reviews pending payouts
    ↓
Owner generates payout report (PDF/CSV)
    ↓
Owner pays provider externally (Venmo, Check, Zelle)
    ↓
Owner marks payout as "Paid" in system
    ↓
Transactions linked to that payout
    ↓
Provider's pending balance returns to $0
```

---

## Data Architecture Decision

### Option A: Calculate on-the-fly (No Payout table until paid)
```
Pending balance = SUM(transactions WHERE payoutId IS NULL AND providerId = X)
```
- Simpler
- Always accurate
- No sync issues

### Option B: Running balance on Provider record
```
Provider.PendingBalance = $245.00 (updated on each transaction)
```
- Faster queries
- Can get out of sync
- Needs recalculation logic

### ✅ Recommendation: Option A (Calculate on-the-fly)

Payout record only created when owner actually pays. Until then, it's just aggregated transactions.

---

## Database Schema

### Payouts Table (New)
```sql
CREATE TABLE Payouts (
    PayoutId UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    OrganizationId UUID NOT NULL REFERENCES Organizations(OrganizationId),
    ProviderId UUID NOT NULL REFERENCES Providers(ProviderId),
    
    -- Payout Details
    PayoutNumber VARCHAR(50) NOT NULL,       -- PAY-2025-001
    PayoutDate DATE NOT NULL,
    Amount DECIMAL(10,2) NOT NULL,
    
    -- Payment Info
    PaymentMethod VARCHAR(50) NOT NULL,      -- Venmo, Zelle, Check, Cash, PayPal
    PaymentReference VARCHAR(100),           -- Venmo txn ID, Check #, etc.
    
    -- Period Covered
    PeriodStart DATE NOT NULL,
    PeriodEnd DATE NOT NULL,
    TransactionCount INT NOT NULL,
    
    Notes TEXT,
    
    -- Audit
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CreatedBy UUID REFERENCES Users(UserId),
    
    CONSTRAINT UQ_Payouts_PayoutNumber UNIQUE (OrganizationId, PayoutNumber)
);

CREATE INDEX idx_payouts_org ON Payouts(OrganizationId);
CREATE INDEX idx_payouts_provider ON Payouts(ProviderId);
CREATE INDEX idx_payouts_date ON Payouts(PayoutDate);
```

### Update Transactions Table
```sql
-- Add to existing Transactions table
ALTER TABLE Transactions 
ADD COLUMN PayoutId UUID REFERENCES Payouts(PayoutId),
ADD COLUMN PayoutStatus VARCHAR(20) DEFAULT 'Pending';  -- Pending, Paid

CREATE INDEX idx_transactions_payout ON Transactions(PayoutId);
CREATE INDEX idx_transactions_payout_status ON Transactions(PayoutStatus);
```

---

## API Endpoints

### PayoutsController
```csharp
[ApiController]
[Route("api/[controller]")]
public class PayoutsController : ControllerBase
{
    // GET PENDING - List providers with pending balances
    [HttpGet("pending")]
    Task<ActionResult<List<PendingPayoutDto>>> GetPendingPayouts();
    
    // GET PENDING DETAIL - Get pending transactions for one provider
    [HttpGet("pending/{providerId}")]
    Task<ActionResult<PendingPayoutDetailDto>> GetPendingPayoutDetail(Guid providerId);
    
    // GET HISTORY - List completed payouts with filters
    [HttpGet]
    Task<ActionResult<PagedResult<PayoutDto>>> GetPayouts(
        [FromQuery] PayoutQueryParams queryParams);
    
    // GET ONE - Get payout by ID
    [HttpGet("{id}")]
    Task<ActionResult<PayoutDto>> GetPayout(Guid id);
    
    // CREATE - Mark payout as paid (creates payout record, links transactions)
    [HttpPost]
    Task<ActionResult<PayoutDto>> CreatePayout(
        [FromBody] CreatePayoutRequest request);
    
    // EXPORT - Generate payout report (PDF or CSV)
    [HttpGet("pending/{providerId}/export")]
    Task<ActionResult> ExportPendingPayout(
        Guid providerId, 
        [FromQuery] string format = "pdf");  // pdf or csv
    
    // EXPORT - Generate completed payout receipt
    [HttpGet("{id}/export")]
    Task<ActionResult> ExportPayoutReceipt(
        Guid id,
        [FromQuery] string format = "pdf");
}
```

### DTOs
```csharp
// Pending payout summary (list view)
public class PendingPayoutDto
{
    public Guid ProviderId { get; set; }
    public string ProviderName { get; set; }
    public string PaymentMethod { get; set; }      // Provider's preferred method
    public string PaymentDetails { get; set; }     // Venmo handle, etc.
    public decimal PendingAmount { get; set; }
    public int TransactionCount { get; set; }
    public DateTime OldestTransactionDate { get; set; }
    public DateTime NewestTransactionDate { get; set; }
}

// Pending payout detail (single provider)
public class PendingPayoutDetailDto
{
    public Guid ProviderId { get; set; }
    public string ProviderName { get; set; }
    public string PaymentMethod { get; set; }
    public string PaymentDetails { get; set; }
    public decimal TotalPending { get; set; }
    public int TransactionCount { get; set; }
    public List<PayoutTransactionDto> Transactions { get; set; }
}

public class PayoutTransactionDto
{
    public Guid TransactionId { get; set; }
    public DateTime SaleDate { get; set; }
    public string ItemName { get; set; }
    public string ItemSku { get; set; }
    public decimal SalePrice { get; set; }
    public decimal CommissionRate { get; set; }
    public decimal ProviderAmount { get; set; }
}

// Completed payout
public class PayoutDto
{
    public Guid PayoutId { get; set; }
    public string PayoutNumber { get; set; }
    public Guid ProviderId { get; set; }
    public string ProviderName { get; set; }
    public DateTime PayoutDate { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; }
    public string PaymentReference { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int TransactionCount { get; set; }
    public string Notes { get; set; }
}

// Create payout request
public class CreatePayoutRequest
{
    public Guid ProviderId { get; set; }
    public DateTime PayoutDate { get; set; }
    public string PaymentMethod { get; set; }
    public string PaymentReference { get; set; }
    public string Notes { get; set; }
    // TransactionIds optional - if null, includes ALL pending for provider
    public List<Guid>? TransactionIds { get; set; }
}

// Query params
public class PayoutQueryParams
{
    public Guid? ProviderId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
```

---

## Wireframe Screens

### Screen 1: Payouts Landing Page (Pending Payouts)
```
┌─────────────────────────────────────────────────────────────────┐
│  ConsignmentGenie    Dashboard  Providers  Inventory  Sales     │
│                                                    [Payouts]  Reports  │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  Payouts                                                        │
│  Manage provider payments                                       │
│                                                                 │
│  [Pending Payouts]    [Payout History]              ← Tabs      │
│  ──────────────────                                             │
│                                                                 │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐            │
│  │ TOTAL PENDING│ │ PROVIDERS    │ │ OLDEST       │            │
│  │   $1,847.50  │ │ AWAITING     │ │ UNPAID       │            │
│  │              │ │     6        │ │ 32 days      │            │
│  └──────────────┘ └──────────────┘ └──────────────┘            │
│                                                                 │
│  Providers Awaiting Payment                                     │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ Provider    │ Pending  │ Items │ Oldest Sale │ Pay Via   │ Actions │
│  ├─────────────┼──────────┼───────┼─────────────┼───────────┼─────────┤
│  │ Jane Doe    │ $487.50  │ 12    │ Oct 20      │ Venmo     │ [View] [Pay] │
│  ├─────────────┼──────────┼───────┼─────────────┼───────────┼─────────┤
│  │ Bob Smith   │ $325.00  │ 8     │ Oct 25      │ Check     │ [View] [Pay] │
│  ├─────────────┼──────────┼───────┼─────────────┼───────────┼─────────┤
│  │ Maria Garcia│ $540.00  │ 15    │ Nov 1       │ Zelle     │ [View] [Pay] │
│  ├─────────────┼──────────┼───────┼─────────────┼───────────┼─────────┤
│  │ Tom Johnson │ $180.00  │ 4     │ Nov 10      │ Venmo     │ [View] [Pay] │
│  ├─────────────┼──────────┼───────┼─────────────┼───────────┼─────────┤
│  │ Sarah W.    │ $215.00  │ 6     │ Nov 5       │ PayPal    │ [View] [Pay] │
│  ├─────────────┼──────────┼───────┼─────────────┼───────────┼─────────┤
│  │ Mike Brown  │ $100.00  │ 2     │ Nov 15      │ Check     │ [View] [Pay] │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                 │
│  Providers with $0 pending balance are not shown                │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘

NOTES:
- "View" opens detail modal with transaction list
- "Pay" opens payment modal to record payout
- Sorted by Oldest Sale (longest waiting first) by default
- Click column headers to sort
```

### Screen 2: Pending Payout Detail Modal
```
┌─────────────────────────────────────────────────────────────────┐
│                                                           [X]   │
│  Pending Payout: Jane Doe                                       │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │                                                          │   │
│  │  Provider:        Jane Doe                               │   │
│  │  Commission Rate: 50%                                    │   │
│  │  Payment Method:  Venmo (@janedoe)                       │   │
│  │                                                          │   │
│  │  ───────────────────────────────────────────────────     │   │
│  │                                                          │   │
│  │  Total Pending:   $487.50                                │   │
│  │  Items Sold:      12                                     │   │
│  │  Period:          Oct 20 - Nov 20, 2025                  │   │
│  │                                                          │   │
│  │  ───────────────────────────────────────────────────     │   │
│  │                                                          │   │
│  │  Transaction Detail                                      │   │
│  │  ┌────────────────────────────────────────────────────┐  │   │
│  │  │ Date    │ Item              │ Sale   │ Provider   │  │   │
│  │  │         │                   │ Price  │ Amount     │  │   │
│  │  ├─────────┼───────────────────┼────────┼────────────┤  │   │
│  │  │ Nov 20  │ Vintage Dress     │ $45.00 │ $22.50     │  │   │
│  │  │ Nov 18  │ Silk Scarf        │ $35.00 │ $17.50     │  │   │
│  │  │ Nov 15  │ Leather Belt      │ $28.00 │ $14.00     │  │   │
│  │  │ Nov 12  │ Cashmere Sweater  │ $95.00 │ $47.50     │  │   │
│  │  │ ...     │ ...               │ ...    │ ...        │  │   │
│  │  └────────────────────────────────────────────────────┘  │   │
│  │                                                          │   │
│  │  ───────────────────────────────────────────────────     │   │
│  │                                                          │   │
│  │  Total Sales:     $975.00                                │   │
│  │  Shop Revenue:    $487.50 (50%)                          │   │
│  │  Provider Payout: $487.50 (50%)                          │   │
│  │                                                          │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│      [Export PDF]   [Export CSV]          [Close]  [Record Payment] │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘

NOTES:
- Export PDF generates a report to give provider
- Export CSV for records/accounting
- "Record Payment" opens payment modal
```

### Screen 3: Record Payment Modal
```
┌─────────────────────────────────────────────────────────────────┐
│                                                           [X]   │
│  Record Payment                                                 │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │                                                          │   │
│  │  Provider:      Jane Doe                                 │   │
│  │  Amount:        $487.50                                  │   │
│  │  Transactions:  12 items                                 │   │
│  │                                                          │   │
│  │  ───────────────────────────────────────────────────     │   │
│  │                                                          │   │
│  │  Payment Date *                                          │   │
│  │  [📅 11/21/2025]                                         │   │
│  │                                                          │   │
│  │  Payment Method *                                        │   │
│  │  [Venmo ▾]                                               │   │
│  │  Options: Venmo, Zelle, PayPal, Check, Cash, Other      │   │
│  │                                                          │   │
│  │  Payment Reference                                       │   │
│  │  [Venmo txn #1234567890      ]                          │   │
│  │  (Check number, transaction ID, etc.)                   │   │
│  │                                                          │   │
│  │  Notes (optional)                                        │   │
│  │  [                                    ]                  │   │
│  │                                                          │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│  ⚠️  This will mark 12 transactions as paid and cannot be      │
│      easily undone.                                             │
│                                                                 │
│                              [Cancel]    [Confirm Payment]      │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘

WORKFLOW:
1. Owner pays provider externally (Venmo, writes check, etc.)
2. Owner opens this modal
3. Enters payment details
4. Clicks "Confirm Payment"
5. System creates Payout record
6. Links all pending transactions to this payout
7. Updates transactions PayoutStatus = 'Paid'
8. Success message, refresh pending list
9. Provider disappears from pending list (balance now $0)
```

### Screen 4: Payout History Tab
```
┌─────────────────────────────────────────────────────────────────┐
│  Payouts                                                        │
│  Manage provider payments                                       │
│                                                                 │
│  [Pending Payouts]    [Payout History]              ← Tabs      │
│                       ────────────────                          │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ Filters                                                  │   │
│  │                                                          │   │
│  │ Provider           Start Date       End Date             │   │
│  │ [All Providers ▾]  [📅 mm/dd/yyyy]  [📅 mm/dd/yyyy]      │   │
│  │                                                          │   │
│  │                                        [Clear Filters]   │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│  Payout History                                    [Export All] │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ Payout #     │ Date    │ Provider    │ Amount  │ Method  │ Actions │
│  ├──────────────┼─────────┼─────────────┼─────────┼─────────┼─────────┤
│  │ PAY-2025-003 │ Nov 21  │ Jane Doe    │ $487.50 │ Venmo   │ [View]  │
│  ├──────────────┼─────────┼─────────────┼─────────┼─────────┼─────────┤
│  │ PAY-2025-002 │ Oct 31  │ Jane Doe    │ $245.00 │ Venmo   │ [View]  │
│  ├──────────────┼─────────┼─────────────┼─────────┼─────────┼─────────┤
│  │ PAY-2025-001 │ Oct 31  │ Bob Smith   │ $180.00 │ Check   │ [View]  │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                 │
│  ← Previous   Page 1 of 1   Next →                              │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘

NOTES:
- "View" opens payout detail with linked transactions
- "Export All" downloads CSV of all payouts (filtered)
- Sorted by date descending (newest first)
```

### Screen 5: Completed Payout Detail Modal
```
┌─────────────────────────────────────────────────────────────────┐
│                                                           [X]   │
│  Payout Details                                                 │
│  PAY-2025-003                                                   │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │                                                          │   │
│  │  Provider:        Jane Doe                               │   │
│  │  Payout Date:     November 21, 2025                      │   │
│  │  Amount:          $487.50                                │   │
│  │                                                          │   │
│  │  Payment Method:  Venmo                                  │   │
│  │  Reference:       Venmo txn #1234567890                  │   │
│  │                                                          │   │
│  │  Period Covered:  Oct 20 - Nov 20, 2025                  │   │
│  │  Transactions:    12 items                               │   │
│  │                                                          │   │
│  │  ───────────────────────────────────────────────────     │   │
│  │                                                          │   │
│  │  Transactions Included                                   │   │
│  │  ┌────────────────────────────────────────────────────┐  │   │
│  │  │ Date    │ Item              │ Sale   │ Provider   │  │   │
│  │  │         │                   │ Price  │ Amount     │  │   │
│  │  ├─────────┼───────────────────┼────────┼────────────┤  │   │
│  │  │ Nov 20  │ Vintage Dress     │ $45.00 │ $22.50     │  │   │
│  │  │ Nov 18  │ Silk Scarf        │ $35.00 │ $17.50     │  │   │
│  │  │ ...     │ ...               │ ...    │ ...        │  │   │
│  │  └────────────────────────────────────────────────────┘  │   │
│  │                                                          │   │
│  │  Notes: Monthly payout for November                      │   │
│  │                                                          │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│                    [Export Receipt]                [Close]      │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## User Flow Storyboard

### Happy Path: Process Monthly Payouts

```
Step 1: Owner navigates to Payouts page
        ↓
        Sees 6 providers with pending balances
        Total: $1,847.50
        ↓
Step 2: Clicks "View" on Jane Doe ($487.50)
        ↓
        Modal shows 12 transactions, date range, totals
        ↓
Step 3: Clicks "Export PDF"
        ↓
        Downloads payout report
        (Can email/text to Jane as receipt)
        ↓
Step 4: Opens Venmo, sends Jane $487.50
        ↓
Step 5: Returns to system, clicks "Record Payment"
        ↓
        Enters:
        - Date: Today
        - Method: Venmo
        - Reference: Venmo txn #1234567890
        ↓
Step 6: Clicks "Confirm Payment"
        ↓
Step 7: System:
        - Creates Payout record (PAY-2025-003)
        - Links 12 transactions to payout
        - Marks transactions PayoutStatus = 'Paid'
        ↓
Step 8: Success! Jane disappears from pending list
        ↓
Step 9: Repeat for other providers...
```

---

## Lookups Needed

### PayoutPaymentMethod (Add to LookupController)
```csharp
public enum PayoutPaymentMethod
{
    Venmo,
    Zelle,
    PayPal,
    Check,
    Cash,
    BankTransfer,
    Other
}
```
*Note: This is separate from transaction PaymentMethod (how customer paid). This is how owner pays provider.*

---

## Dashboard Integration

Update Dashboard cards:
- **"Pending Payouts"** → Links to `/owner/payouts`
- Shows total pending amount
- Shows provider count waiting

---

## Export Formats

### PDF Payout Report (for provider)
```
┌─────────────────────────────────────────────┐
│  PAYOUT REPORT                              │
│  Demo Consignment Shop                      │
│                                             │
│  Provider: Jane Doe                         │
│  Period: Oct 20 - Nov 20, 2025              │
│  Generated: Nov 21, 2025                    │
│                                             │
│  ─────────────────────────────────────────  │
│                                             │
│  ITEMS SOLD                                 │
│                                             │
│  Date       Item                 Your Share │
│  Nov 20     Vintage Dress        $22.50     │
│  Nov 18     Silk Scarf           $17.50     │
│  Nov 15     Leather Belt         $14.00     │
│  ...                                        │
│                                             │
│  ─────────────────────────────────────────  │
│                                             │
│  Total Sales:        $975.00                │
│  Your Commission:    50%                    │
│  YOUR PAYOUT:        $487.50                │
│                                             │
│  ─────────────────────────────────────────  │
│                                             │
│  Thank you for consigning with us!          │
│                                             │
└─────────────────────────────────────────────┘
```

### CSV Export (for accounting/QuickBooks)
```csv
PayoutNumber,PayoutDate,ProviderId,ProviderName,Amount,PaymentMethod,Reference,TransactionCount
PAY-2025-003,2025-11-21,uuid-here,Jane Doe,487.50,Venmo,txn#1234567890,12
PAY-2025-002,2025-10-31,uuid-here,Jane Doe,245.00,Venmo,txn#9876543210,8
```

---

## Action Items for CC

### Database:
1. Create `Payouts` table per schema above
2. Add `PayoutId` and `PayoutStatus` columns to `Transactions` table
3. Create indexes

### API:
1. Create `PayoutsController` with all endpoints
2. Create DTOs as specified
3. Add `PayoutPaymentMethod` to LookupController
4. Implement calculate-on-the-fly for pending balances
5. Implement PDF export (basic, can enhance later)
6. Implement CSV export

### UI:
1. Create Payouts page with tabs (Pending / History)
2. Build pending payouts list
3. Build pending payout detail modal
4. Build record payment modal
5. Build payout history list with filters
6. Build completed payout detail modal
7. Wire up exports

### Integration:
1. Update Dashboard "Pending Payouts" card to use real data
2. Link card to `/owner/payouts`

---

## Success Criteria

- [ ] Owner can see list of providers with pending balances
- [ ] Owner can view transaction detail for a pending payout
- [ ] Owner can export PDF payout report
- [ ] Owner can export CSV
- [ ] Owner can record a payment (creates payout, links transactions)
- [ ] Paid transactions no longer appear in pending
- [ ] Owner can view payout history
- [ ] Owner can filter payout history by provider/date
- [ ] Owner can view completed payout details
- [ ] Dashboard shows real pending payout total

---

*Document Version: 1.0*
*Last Updated: November 21, 2025*
