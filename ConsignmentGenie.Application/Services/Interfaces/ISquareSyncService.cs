using ConsignmentGenie.Core.DTOs.Square;

namespace ConsignmentGenie.Application.Services.Interfaces;

public interface ISquareSyncService
{
    /// <summary>
    /// Sync all categories from Square. Creates/updates CG category mappings.
    /// Called automatically by SyncAllAsync and SyncItemAsync.
    /// </summary>
    Task<CategorySyncResult> SyncCategoriesAsync(Guid shopId);

    /// <summary>
    /// Full catalog sync. Use after OAuth connect, manual refresh, or scheduled job.
    /// Calls SyncCategoriesAsync first, then syncs all items.
    /// </summary>
    Task<FullSyncResult> SyncAllAsync(Guid shopId);

    /// <summary>
    /// Single item sync. Use for add-to-cart validation, item refresh.
    /// Ensures item's category exists, then syncs just that item.
    /// Returns current availability status.
    /// </summary>
    Task<ItemSyncResult> SyncItemAsync(Guid shopId, string squareItemId);

    /// <summary>
    /// Sync Square transactions/payments. Imports recent transactions and creates pending
    /// transactions for items that are not yet assigned to consignors.
    /// </summary>
    Task<TransactionSyncResult> SyncTransactionsAsync(Guid shopId);
}