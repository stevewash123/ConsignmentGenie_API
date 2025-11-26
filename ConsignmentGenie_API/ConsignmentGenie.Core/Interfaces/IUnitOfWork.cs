using ConsignmentGenie.Core.Entities;

namespace ConsignmentGenie.Core.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IRepository<User> Users { get; }
    IRepository<Organization> Organizations { get; }
    IRepository<Provider> Providers { get; }
    IRepository<Item> Items { get; }
    IRepository<Transaction> Transactions { get; }
    IRepository<Payout> Payouts { get; }
    IRepository<SubscriptionEvent> SubscriptionEvents { get; }
    IRepository<SquareConnection> SquareConnections { get; }
    IRepository<SquareSyncLog> SquareSyncLogs { get; }
    IRepository<PaymentGatewayConnection> PaymentGatewayConnections { get; }
    IRepository<ItemCategory> ItemCategories { get; }
    IRepository<ItemTag> ItemTags { get; }
    IRepository<ItemTagAssignment> ItemTagAssignments { get; }
    IRepository<AuditLog> AuditLogs { get; }
    IRepository<Notification> Notifications { get; }

    Task<int> SaveChangesAsync();
}