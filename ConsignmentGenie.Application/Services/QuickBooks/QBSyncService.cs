using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Services;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ConsignmentGenie.Application.Services.QuickBooks;

public class QBSyncService : IQBSyncService
{
    private readonly ConsignmentGenieContext _context;
    private readonly IQuickBooksApiService _qbApi;
    private readonly IJobNotificationService _notifications;
    private readonly ILogger<QBSyncService> _logger;

    public QBSyncService(
        ConsignmentGenieContext context,
        IQuickBooksApiService qbApi,
        IJobNotificationService notifications,
        ILogger<QBSyncService> logger)
    {
        _context = context;
        _qbApi = qbApi;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task SyncSale(Guid transactionId)
    {
        var transaction = await _context.Transactions
            .Include(t => t.Items)
                .ThenInclude(ti => ti.Item)
            .FirstOrDefaultAsync(t => t.Id == transactionId);

        if (transaction == null)
        {
            await LogError(transactionId, "Transaction", "CreateSalesReceipt", "Transaction not found");
            return;
        }

        var settings = await GetBookkeepingSettings(transaction.OrganizationId);
        if (!ShouldSyncSales(settings)) return;

        try
        {
            var salesReceipt = BuildQBSalesReceipt(transaction, settings);
            var result = await _qbApi.CreateSalesReceipt(salesReceipt, settings.QuickBooksCompanyId!);

            if (result.Success)
            {
                transaction.QBSalesReceiptId = result.QBId;
                transaction.SyncedToQuickBooks = true;
                transaction.SyncedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                await LogSuccess(transactionId, "Transaction", "CreateSalesReceipt", result.QBId!);
                await _notifications.NotifySuccess(
                    transaction.OrganizationId,
                    "Sale Synced to QuickBooks",
                    $"Transaction #{transaction.Id} recorded in QuickBooks"
                );
            }
            else
            {
                await LogError(transactionId, "Transaction", "CreateSalesReceipt", result.ErrorMessage!);
                await _notifications.NotifyError(
                    transaction.OrganizationId,
                    "Sale Sync Failed",
                    result.ErrorMessage!
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync sale {TransactionId} to QuickBooks", transactionId);
            await LogError(transactionId, "Transaction", "CreateSalesReceipt", ex.Message);
            await _notifications.NotifyError(
                transaction.OrganizationId,
                "Sale Sync Failed",
                $"Transaction sync failed: {ex.Message}"
            );
        }
    }

    public async Task SyncVendor(Guid consignorId)
    {
        var consignor = await _context.Consignors.FirstOrDefaultAsync(c => c.Id == consignorId);
        if (consignor == null)
        {
            await LogError(consignorId, "Consignor", "CreateVendor", "Consignor not found");
            return;
        }

        var settings = await GetBookkeepingSettings(consignor.OrganizationId);
        if (!ShouldSyncVendors(settings)) return;

        try
        {
            var vendor = BuildQBVendor(consignor);
            var result = await _qbApi.CreateVendor(vendor, settings.QuickBooksCompanyId!);

            if (result.Success)
            {
                consignor.QBVendorId = result.QBId;
                await _context.SaveChangesAsync();

                await LogSuccess(consignorId, "Consignor", "CreateVendor", result.QBId!);
                await _notifications.NotifySuccess(
                    consignor.OrganizationId,
                    "Consignor Synced to QuickBooks",
                    $"Consignor {consignor.Name} {consignor} added as vendor"
                );
            }
            else
            {
                await LogError(consignorId, "Consignor", "CreateVendor", result.ErrorMessage!);
                await _notifications.NotifyError(
                    consignor.OrganizationId,
                    "Consignor Sync Failed",
                    result.ErrorMessage!
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync consignor {ConsignorId} to QuickBooks", consignorId);
            await LogError(consignorId, "Consignor", "CreateVendor", ex.Message);
        }
    }

    public async Task SyncPayoutCreated(Guid payoutId)
    {
        var payout = await _context.Payouts
            .Include(p => p.Consignor)
            .Include(p => p.Transactions)
            .FirstOrDefaultAsync(p => p.Id == payoutId);

        if (payout == null) return;

        var settings = await GetBookkeepingSettings(payout.OrganizationId);
        if (!ShouldSyncPayouts(settings) || settings.PayoutRecordingMethod != "bill-payment") return;

        try
        {
            // Ensure consignor exists as vendor
            await EnsureVendorExists(payout.ConsignorId);

            var bill = BuildQBBill(payout, settings);
            var result = await _qbApi.CreateBill(bill, settings.QuickBooksCompanyId!);

            if (result.Success)
            {
                payout.QBBillId = result.QBId;
                payout.QBSyncStatus = Core.Enums.QBSyncStatus.Synced;
                await _context.SaveChangesAsync();

                await LogSuccess(payoutId, "Payout", "CreateBill", result.QBId!);
            }
            else
            {
                await LogError(payoutId, "Payout", "CreateBill", result.ErrorMessage!);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync payout bill {PayoutId} to QuickBooks", payoutId);
            await LogError(payoutId, "Payout", "CreateBill", ex.Message);
        }
    }

    public async Task SyncPayoutPaid(Guid payoutId)
    {
        var payout = await _context.Payouts
            .Include(p => p.Consignor)
            .Include(p => p.Transactions)
            .FirstOrDefaultAsync(p => p.Id == payoutId);

        if (payout == null) return;

        var settings = await GetBookkeepingSettings(payout.OrganizationId);
        if (!ShouldSyncPayouts(settings)) return;

        try
        {
            if (settings.PayoutRecordingMethod == "bill-payment")
            {
                await SyncBillPayment(payout, settings);
            }
            else // direct-expense
            {
                await SyncDirectExpense(payout, settings);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync payout payment {PayoutId} to QuickBooks", payoutId);
            await LogError(payoutId, "Payout", "SyncPayment", ex.Message);
        }
    }

    public async Task<bool> EnsureVendorExists(Guid consignorId)
    {
        var consignor = await _context.Consignors.FirstOrDefaultAsync(c => c.Id == consignorId);
        if (consignor?.QBVendorId != null) return true;

        await SyncVendor(consignorId);

        // Refresh to check if sync succeeded
        await _context.Entry(consignor!).ReloadAsync();
        return consignor.QBVendorId != null;
    }

    public async Task<string?> GetQBVendorId(Guid consignorId)
    {
        var consignor = await _context.Consignors.FirstOrDefaultAsync(c => c.Id == consignorId);
        return consignor?.QBVendorId;
    }

    private async Task SyncBillPayment(Payout payout, BookkeepingSettings settings)
    {
        if (string.IsNullOrEmpty(payout.QBBillId)) return;

        var billPayment = BuildQBBillPayment(payout, settings);
        var result = await _qbApi.CreateBillPayment(billPayment, settings.QuickBooksCompanyId!);

        if (result.Success)
        {
            payout.QBPaymentId = result.QBId;
            payout.QBSyncStatus = Core.Enums.QBSyncStatus.Synced;
            await _context.SaveChangesAsync();

            await LogSuccess(payout.Id, "Payout", "CreateBillPayment", result.QBId!);
            await _notifications.NotifySuccess(
                payout.OrganizationId,
                "Payout Recorded in QuickBooks",
                $"Payout {payout.PayoutNumber} payment recorded"
            );
        }
        else
        {
            await LogError(payout.Id, "Payout", "CreateBillPayment", result.ErrorMessage!);
            await _notifications.NotifyError(
                payout.OrganizationId,
                "Payout Sync Failed",
                result.ErrorMessage!
            );
        }
    }

    private async Task SyncDirectExpense(Payout payout, BookkeepingSettings settings)
    {
        var expense = BuildQBExpense(payout, settings);
        var result = await _qbApi.CreateExpense(expense, settings.QuickBooksCompanyId!);

        if (result.Success)
        {
            payout.QBPaymentId = result.QBId;
            payout.QBSyncStatus = Core.Enums.QBSyncStatus.Synced;
            await _context.SaveChangesAsync();

            await LogSuccess(payout.Id, "Payout", "CreateExpense", result.QBId!);
            await _notifications.NotifySuccess(
                payout.OrganizationId,
                "Payout Recorded in QuickBooks",
                $"Payout {payout.PayoutNumber} expense recorded"
            );
        }
        else
        {
            await LogError(payout.Id, "Payout", "CreateExpense", result.ErrorMessage!);
            await _notifications.NotifyError(
                payout.OrganizationId,
                "Payout Sync Failed",
                result.ErrorMessage!
            );
        }
    }

    private QBVendor BuildQBVendor(Consignor consignor)
    {
        return new QBVendor
        {
            DisplayName = $"{consignor.Name} {consignor}",
            Email = consignor.Email,
            Phone = consignor.Phone,
            BillAddr = BuildQBAddress(consignor)
        };
    }

    private QBAddress? BuildQBAddress(Consignor consignor)
    {
        if (string.IsNullOrEmpty(consignor.AddressLine1)) return null;

        return new QBAddress
        {
            Line1 = consignor.AddressLine1,
            Line2 = consignor.AddressLine2,
            City = consignor.City,
            CountrySubDivisionCode = consignor.State,
            PostalCode = consignor.PostalCode,
            Country = "USA"
        };
    }

    private QBBill BuildQBBill(Payout payout, BookkeepingSettings settings)
    {
        var lines = settings.LineItemDetail == "itemized"
            ? BuildItemizedLines(payout)
            : new List<QBBillLine> { new() { Amount = payout.Amount, Description = "Consignment payout" } };

        return new QBBill
        {
            VendorRef = payout.Consignor.QBVendorId!,
            TxnDate = payout.PayoutDate,
            TotalAmt = payout.Amount,
            DocNumber = payout.PayoutNumber,
            PrivateNote = $"Consignment payout for period {payout.PeriodStart:MM/dd/yyyy} - {payout.PeriodEnd:MM/dd/yyyy}",
            Lines = lines
        };
    }

    private QBBillPayment BuildQBBillPayment(Payout payout, BookkeepingSettings settings)
    {
        return new QBBillPayment
        {
            VendorRef = payout.Consignor.QBVendorId!,
            TxnDate = payout.PaidAt ?? DateTime.UtcNow,
            TotalAmt = payout.Amount,
            CheckNum = payout.PaymentReference,
            LinkedTxns = new List<QBLinkedTxn>
            {
                new() { TxnId = payout.QBBillId!, TxnType = "Bill", TxnLineId = 1 }
            }
        };
    }

    private QBExpense BuildQBExpense(Payout payout, BookkeepingSettings settings)
    {
        var lines = settings.LineItemDetail == "itemized"
            ? BuildExpenseLines(payout)
            : new List<QBExpenseLine> { new() { Amount = payout.Amount, Description = "Consignment payout", AccountRef = "Consigner Expense" } };

        return new QBExpense
        {
            VendorRef = payout.Consignor.QBVendorId!,
            TxnDate = payout.PaidAt ?? DateTime.UtcNow,
            TotalAmt = payout.Amount,
            DocNumber = payout.PayoutNumber,
            PrivateNote = $"Consignment payout for period {payout.PeriodStart:MM/dd/yyyy} - {payout.PeriodEnd:MM/dd/yyyy}",
            PaymentType = GetPaymentType(payout.PaymentMethod),
            Lines = lines
        };
    }

    private QBSalesReceipt BuildQBSalesReceipt(Transaction transaction, BookkeepingSettings settings)
    {
        var lines = transaction.Items?.Select(item => new QBSalesReceiptLine
        {
            Amount = item.UnitPrice * item.Quantity,
            Description = item.Item.Description ?? item.Item.Title,
            Qty = item.Quantity
        }).ToList() ?? new List<QBSalesReceiptLine>();

        return new QBSalesReceipt
        {
            TxnDate = transaction.TransactionDate,
            TotalAmt = transaction.Total,
            DocNumber = transaction.Id.ToString(),
            PrivateNote = "ConsignmentGenie sale",
            PaymentMethodRef = MapPaymentMethod(transaction.PaymentType),
            Lines = lines
        };
    }

    private List<QBBillLine> BuildItemizedLines(Payout payout)
    {
        return payout.Transactions?.SelectMany(t => t.Items?.Select(item => new QBBillLine
        {
            Amount = item.UnitPrice * item.Quantity * item.ConsignorSplitPercentage,
            Description = $"{item.Item.Title} - {item.Item.Description}"
        }) ?? Enumerable.Empty<QBBillLine>()).ToList() ?? new List<QBBillLine>();
    }

    private List<QBExpenseLine> BuildExpenseLines(Payout payout)
    {
        return payout.Transactions?.SelectMany(t => t.Items?.Select(item => new QBExpenseLine
        {
            Amount = item.UnitPrice * item.Quantity * item.ConsignorSplitPercentage,
            Description = $"{item.Item.Title} - {item.Item.Description}",
            AccountRef = "Consigner Expense"
        }) ?? Enumerable.Empty<QBExpenseLine>()).ToList() ?? new List<QBExpenseLine>();
    }

    private string GetPaymentType(string paymentMethod) => paymentMethod.ToLowerInvariant() switch
    {
        "cash" => "Cash",
        "check" => "Check",
        "ach" => "Check",
        _ => "Check"
    };

    private string? MapPaymentMethod(string paymentType) => paymentType.ToLowerInvariant() switch
    {
        "cash" => "Cash",
        "card" => "Credit Card",
        "check" => "Check",
        _ => null
    };

    private async Task<BookkeepingSettings?> GetBookkeepingSettings(Guid organizationId)
    {
        return await _context.BookkeepingSettings
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId);
    }

    private bool ShouldSyncSales(BookkeepingSettings? settings) =>
        settings?.UseQuickBooks == true &&
        settings.QuickBooksConnected &&
        settings.SalesSyncEnabled;

    private bool ShouldSyncVendors(BookkeepingSettings? settings) =>
        settings?.UseQuickBooks == true &&
        settings.QuickBooksConnected &&
        settings.ConsignorSyncEnabled;

    private bool ShouldSyncPayouts(BookkeepingSettings? settings) =>
        settings?.UseQuickBooks == true &&
        settings.QuickBooksConnected;

    private async Task LogSuccess(Guid entityId, string entityType, string action, string qbId)
    {
        var log = new QBSyncLog
        {
            ShopId = (await _context.Transactions.FirstOrDefaultAsync(t => t.Id == entityId))?.OrganizationId ??
                    (await _context.Consignors.FirstOrDefaultAsync(c => c.Id == entityId))?.OrganizationId ??
                    (await _context.Payouts.FirstOrDefaultAsync(p => p.Id == entityId))?.OrganizationId ??
                    Guid.Empty,
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            QBId = qbId,
            Success = true
        };

        _context.QBSyncLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    private async Task LogError(Guid entityId, string entityType, string action, string error)
    {
        var log = new QBSyncLog
        {
            ShopId = (await _context.Transactions.FirstOrDefaultAsync(t => t.Id == entityId))?.OrganizationId ??
                    (await _context.Consignors.FirstOrDefaultAsync(c => c.Id == entityId))?.OrganizationId ??
                    (await _context.Payouts.FirstOrDefaultAsync(p => p.Id == entityId))?.OrganizationId ??
                    Guid.Empty,
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            Success = false,
            ErrorMessage = error
        };

        _context.QBSyncLogs.Add(log);
        await _context.SaveChangesAsync();
    }
}