using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Interfaces;
using ConsignmentGenie.Infrastructure.Data;

namespace ConsignmentGenie.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ConsignmentGenieContext _context;

    public UnitOfWork(ConsignmentGenieContext context)
    {
        _context = context;
        Users = new Repository<User>(_context);
        Organizations = new Repository<Organization>(_context);
        Consignors = new Repository<Consignor>(_context);
        ConsignorAgreements = new Repository<ConsignorAgreement>(_context);
        Items = new Repository<Item>(_context);
        Transactions = new Repository<Transaction>(_context);
        TransactionItems = new Repository<TransactionItem>(_context);
        Payouts = new Repository<Payout>(_context);
        SubscriptionEvents = new Repository<SubscriptionEvent>(_context);
        SquareConnections = new Repository<SquareConnection>(_context);
        SquareSyncLogs = new Repository<SquareSyncLog>(_context);
        PendingSquareImports = new Repository<PendingSquareImport>(_context);
        PendingSquareTransactions = new Repository<PendingSquareTransaction>(_context);
        PendingSquareTransactionItems = new Repository<PendingSquareTransactionItem>(_context);
        PaymentGatewayConnections = new Repository<PaymentGatewayConnection>(_context);
        ItemCategories = new Repository<ItemCategory>(_context);
        ItemTags = new Repository<ItemTag>(_context);
        ItemTagAssignments = new Repository<ItemTagAssignment>(_context);
        AuditLogs = new Repository<AuditLog>(_context);
        Notifications = new Repository<Notification>(_context);
        DropoffRequests = new Repository<DropoffRequest>(_context);
    }

    public IRepository<User> Users { get; private set; }
    public IRepository<Organization> Organizations { get; private set; }
    public IRepository<Consignor> Consignors { get; private set; }
    public IRepository<ConsignorAgreement> ConsignorAgreements { get; private set; }
    public IRepository<Item> Items { get; private set; }
    public IRepository<Transaction> Transactions { get; private set; }
    public IRepository<TransactionItem> TransactionItems { get; private set; }
    public IRepository<Payout> Payouts { get; private set; }
    public IRepository<SubscriptionEvent> SubscriptionEvents { get; private set; }
    public IRepository<SquareConnection> SquareConnections { get; private set; }
    public IRepository<SquareSyncLog> SquareSyncLogs { get; private set; }
    public IRepository<PendingSquareImport> PendingSquareImports { get; private set; }
    public IRepository<PendingSquareTransaction> PendingSquareTransactions { get; private set; }
    public IRepository<PendingSquareTransactionItem> PendingSquareTransactionItems { get; private set; }
    public IRepository<PaymentGatewayConnection> PaymentGatewayConnections { get; private set; }
    public IRepository<ItemCategory> ItemCategories { get; private set; }
    public IRepository<ItemTag> ItemTags { get; private set; }
    public IRepository<ItemTagAssignment> ItemTagAssignments { get; private set; }
    public IRepository<AuditLog> AuditLogs { get; private set; }
    public IRepository<Notification> Notifications { get; private set; }
    public IRepository<DropoffRequest> DropoffRequests { get; private set; }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}