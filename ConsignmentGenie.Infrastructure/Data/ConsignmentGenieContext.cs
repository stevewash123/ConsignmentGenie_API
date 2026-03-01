using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace ConsignmentGenie.Infrastructure.Data;

public class ConsignmentGenieContext : DbContext
{
    public ConsignmentGenieContext(DbContextOptions<ConsignmentGenieContext> options) : base(options)
    {
    }

    public DbSet<Organization> Organizations { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<UserRoleAssignment> UserRoleAssignments { get; set; }
    public DbSet<Consignor> Consignors { get; set; }
    public DbSet<Item> Items { get; set; }
    public DbSet<ItemImage> ItemImages { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<TransactionItem> TransactionItems { get; set; }
    public DbSet<Payout> Payouts { get; set; }
    public DbSet<SubscriptionEvent> SubscriptionEvents { get; set; }
    public DbSet<SquareConnection> SquareConnections { get; set; }
    public DbSet<SquareSyncLog> SquareSyncLogs { get; set; }
    public DbSet<PendingSquareImport> PendingSquareImports { get; set; }
    public DbSet<PendingSquareTransaction> PendingSquareTransactions { get; set; }
    public DbSet<PendingSquareTransactionItem> PendingSquareTransactionItems { get; set; }
    public DbSet<PendingImportItem> PendingImportItems { get; set; }
    public DbSet<PaymentGatewayConnection> PaymentGatewayConnections { get; set; }
    public DbSet<ItemCategory> ItemCategories { get; set; }
    public DbSet<ItemTag> ItemTags { get; set; }
    public DbSet<ItemTagAssignment> ItemTagAssignments { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<Suggestion> Suggestions { get; set; }
    public DbSet<UserNotificationPreference> UserNotificationPreferences { get; set; }
    public DbSet<NotificationPreferences> NotificationPreferences { get; set; }
    public DbSet<Statement> Statements { get; set; }
    public DbSet<ItemRequest> ItemRequests { get; set; }
    public DbSet<ItemRequestImage> ItemRequestImages { get; set; }
    public DbSet<DropoffRequest> DropoffRequests { get; set; }
    public DbSet<PendingConsolidatedNotification> PendingConsolidatedNotifications { get; set; }

    public DbSet<IntegrationCredentials> IntegrationCredentials { get; set; }

    // Agreement Management entities
    public DbSet<ConsignorAgreement> ConsignorAgreements { get; set; }
    public DbSet<ConsignorInvitation> ConsignorInvitations { get; set; }
    public DbSet<OwnerInvitation> OwnerInvitations { get; set; }
    public DbSet<ClerkInvitation> ClerkInvitations { get; set; }
    public DbSet<ClerkPermissions> ClerkPermissions { get; set; }
    public DbSet<SupportTicket> SupportTickets { get; set; }
    public DbSet<FileUploadHash> FileUploadHashes { get; set; }
    public DbSet<ConsignorAgreementUpload> ConsignorAgreementUploads { get; set; }
    public DbSet<PayoutSettings> PayoutSettings { get; set; }

    // ACH Direct Deposit entities
    public DbSet<FundingSource> FundingSources { get; set; }
    public DbSet<AchTransfer> AchTransfers { get; set; }
    public DbSet<PayoutSummary> PayoutSummaries { get; set; }
    public DbSet<BookkeepingSettings> BookkeepingSettings { get; set; }
    public DbSet<QBSyncLog> QBSyncLogs { get; set; }

    // Reservation System entities
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Reservation> Reservations { get; set; }
    public DbSet<ReservationItem> ReservationItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Organization configuration
        modelBuilder.Entity<Organization>(entity =>
        {
            entity.HasIndex(o => o.Name);
            entity.HasIndex(o => o.Subdomain).IsUnique();
            entity.HasIndex(o => o.Slug).IsUnique();
        });

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasOne(u => u.Organization)
                  .WithMany(o => o.Users)
                  .HasForeignKey(u => u.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // UserRoleAssignment configuration
        modelBuilder.Entity<UserRoleAssignment>(entity =>
        {
            entity.HasIndex(ura => ura.UserId);
            entity.HasIndex(ura => ura.OrganizationId);
            entity.HasIndex(ura => new { ura.UserId, ura.Role, ura.OrganizationId }).IsUnique();
            entity.HasIndex(ura => new { ura.UserId, ura.IsActive });

            entity.HasOne(ura => ura.User)
                  .WithMany(u => u.RoleAssignments)
                  .HasForeignKey(ura => ura.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ura => ura.Organization)
                  .WithMany()
                  .HasForeignKey(ura => ura.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Consignor configuration
        modelBuilder.Entity<Consignor>(entity =>
        {
            entity.HasIndex(p => new { p.OrganizationId, p.ConsignorNumber }).IsUnique();
            entity.HasIndex(p => new { p.OrganizationId, p.Email }).IsUnique();
            entity.HasIndex(p => new { p.OrganizationId, p.Status });
            entity.HasIndex(p => p.ApprovalStatus);

            entity.HasOne(p => p.Organization)
                  .WithMany(o => o.Consignors)
                  .HasForeignKey(p => p.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(p => p.User)
                  .WithOne(u => u.Consignor)
                  .HasForeignKey<Consignor>(p => p.UserId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(p => p.ApprovedByUser)
                  .WithMany()
                  .HasForeignKey(p => p.ApprovedBy)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(p => p.CreatedByUser)
                  .WithMany()
                  .HasForeignKey(p => p.CreatedBy)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(p => p.UpdatedByUser)
                  .WithMany()
                  .HasForeignKey(p => p.UpdatedBy)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Item configuration
        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasIndex(i => new { i.OrganizationId, i.Sku }).IsUnique();
            entity.HasIndex(i => new { i.OrganizationId, i.Status });
            entity.HasIndex(i => new { i.OrganizationId, i.ItemCategoryId });
            entity.HasOne(i => i.Organization)
                  .WithMany(o => o.Items)
                  .HasForeignKey(i => i.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(i => i.Consignor)
                  .WithMany(p => p.Items)
                  .HasForeignKey(i => i.ConsignorId)
                  .OnDelete(DeleteBehavior.Restrict);
            // ItemCategory relationship configured from ItemCategory side to prevent shadow properties
            entity.HasOne(i => i.CreatedByUser)
                  .WithMany()
                  .HasForeignKey(i => i.CreatedBy)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(i => i.UpdatedByUser)
                  .WithMany()
                  .HasForeignKey(i => i.UpdatedBy)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Transaction configuration
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasIndex(t => t.OrganizationId);
            entity.HasIndex(t => t.SaleDate);
            entity.HasOne(t => t.Organization)
                  .WithMany(o => o.Transactions)
                  .HasForeignKey(t => t.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(t => t.Items)
                  .WithOne(ti => ti.Transaction)
                  .HasForeignKey(ti => ti.TransactionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // TransactionItem configuration
        modelBuilder.Entity<TransactionItem>(entity =>
        {
            entity.HasIndex(ti => ti.OrganizationId);
            entity.HasIndex(ti => ti.TransactionId);
            entity.HasIndex(ti => ti.ItemId);
            entity.HasIndex(ti => ti.ConsignorId);

            entity.HasOne(ti => ti.Organization)
                  .WithMany()
                  .HasForeignKey(ti => ti.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ti => ti.Item)
                  .WithMany()
                  .HasForeignKey(ti => ti.ItemId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(ti => ti.Consignor)
                  .WithMany()
                  .HasForeignKey(ti => ti.ConsignorId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.Ignore(ti => ti.LineTotal); // Computed property
        });

        // Payout configuration
        modelBuilder.Entity<Payout>(entity =>
        {
            entity.HasIndex(p => p.OrganizationId);
            entity.HasIndex(p => new { p.ConsignorId, p.PeriodStart, p.PeriodEnd });
            entity.HasOne(p => p.Organization)
                  .WithMany(o => o.Payouts)
                  .HasForeignKey(p => p.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(p => p.Consignor)
                  .WithMany(pr => pr.Payouts)
                  .HasForeignKey(p => p.ConsignorId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // SubscriptionEvent configuration
        modelBuilder.Entity<SubscriptionEvent>(entity =>
        {
            entity.HasIndex(s => s.StripeEventId).IsUnique();
            entity.HasIndex(s => s.OrganizationId);
            entity.HasOne(s => s.Organization)
                  .WithMany()
                  .HasForeignKey(s => s.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // SquareConnection configuration
        modelBuilder.Entity<SquareConnection>(entity =>
        {
            entity.HasIndex(s => s.OrganizationId).IsUnique();
            entity.Property(s => s.OrganizationId).IsRequired();
            entity.HasOne(s => s.Organization)
                  .WithOne(o => o.SquareConnection)
                  .HasForeignKey<SquareConnection>(s => s.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // SquareSyncLog configuration
        modelBuilder.Entity<SquareSyncLog>(entity =>
        {
            entity.HasIndex(s => s.OrganizationId);
            entity.HasIndex(s => s.SquareConnectionId);
            entity.HasIndex(s => s.SyncStarted);
            // Only configure relationship to SquareConnection, Organization access via SquareConnection.Organization
            entity.HasOne(s => s.SquareConnection)
                  .WithMany()
                  .HasForeignKey(s => s.SquareConnectionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // PendingSquareImport configuration
        modelBuilder.Entity<PendingSquareImport>(entity =>
        {
            entity.HasIndex(p => p.OrganizationId);
            entity.HasIndex(p => new { p.OrganizationId, p.SquareCatalogId }).IsUnique();
            entity.HasIndex(p => p.ImportedAt);
            entity.HasOne(p => p.Organization)
                  .WithMany()
                  .HasForeignKey(p => p.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // PendingSquareTransaction configuration
        modelBuilder.Entity<PendingSquareTransaction>(entity =>
        {
            entity.HasIndex(p => p.OrganizationId);
            entity.HasIndex(p => new { p.OrganizationId, p.SquarePaymentId }).IsUnique();
            entity.HasIndex(p => p.TransactionDate);
            entity.HasIndex(p => p.Status);
            entity.HasOne(p => p.Organization)
                  .WithMany()
                  .HasForeignKey(p => p.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // PendingSquareTransactionItem configuration
        modelBuilder.Entity<PendingSquareTransactionItem>(entity =>
        {
            entity.HasIndex(p => p.OrganizationId);
            entity.HasIndex(p => p.PendingTransactionId);
            entity.HasIndex(p => new { p.OrganizationId, p.SquareCatalogId });
            entity.HasIndex(p => p.PendingSquareImportId);
            entity.HasIndex(p => p.ItemId);

            entity.HasOne(p => p.Organization)
                  .WithMany()
                  .HasForeignKey(p => p.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(p => p.PendingTransaction)
                  .WithMany(t => t.Items)
                  .HasForeignKey(p => p.PendingTransactionId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(p => p.PendingSquareImport)
                  .WithMany()
                  .HasForeignKey(p => p.PendingSquareImportId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(p => p.Item)
                  .WithMany()
                  .HasForeignKey(p => p.ItemId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Transaction - Add Square index
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasIndex(t => t.SquarePaymentId).IsUnique();

            // Clerk audit trail relationship
            entity.HasOne(t => t.ProcessedByUser)
                  .WithMany()
                  .HasForeignKey(t => t.ProcessedByUserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // ClerkPermissions configuration
        modelBuilder.Entity<ClerkPermissions>(entity =>
        {
            entity.HasIndex(cp => cp.OrganizationId).IsUnique();
            entity.HasOne(cp => cp.Organization)
                  .WithMany()
                  .HasForeignKey(cp => cp.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // PaymentGatewayConnection configuration
        modelBuilder.Entity<PaymentGatewayConnection>(entity =>
        {
            entity.HasIndex(p => p.OrganizationId);
            entity.HasIndex(p => new { p.OrganizationId, p.Provider, p.IsActive });
            entity.HasIndex(p => new { p.OrganizationId, p.IsDefault });
            entity.HasOne(p => p.Organization)
                  .WithMany()
                  .HasForeignKey(p => p.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ItemCategory configuration
        modelBuilder.Entity<ItemCategory>(entity =>
        {
            entity.HasIndex(c => c.OrganizationId);
            entity.HasIndex(c => new { c.OrganizationId, c.Name }).IsUnique();
            entity.HasIndex(c => c.ParentCategoryId);
            entity.HasOne(c => c.Organization)
                  .WithMany()
                  .HasForeignKey(c => c.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(c => c.ParentCategory)
                  .WithMany(c => c.SubCategories)
                  .HasForeignKey(c => c.ParentCategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
            // Configure the reverse relationship to Items - this prevents EF shadow properties
            entity.HasMany(c => c.Items)
                  .WithOne(i => i.ItemCategoryNavigation)
                  .HasForeignKey(i => i.ItemCategoryId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // ItemTag configuration
        modelBuilder.Entity<ItemTag>(entity =>
        {
            entity.HasIndex(t => t.OrganizationId);
            entity.HasIndex(t => new { t.OrganizationId, t.Name }).IsUnique();
            entity.HasOne(t => t.Organization)
                  .WithMany()
                  .HasForeignKey(t => t.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ItemTagAssignment configuration (junction table)
        modelBuilder.Entity<ItemTagAssignment>(entity =>
        {
            entity.HasKey(ita => new { ita.ItemId, ita.ItemTagId });
            entity.HasOne(ita => ita.Item)
                  .WithMany()
                  .HasForeignKey(ita => ita.ItemId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(ita => ita.ItemTag)
                  .WithMany(it => it.ItemTagAssignments)
                  .HasForeignKey(ita => ita.ItemTagId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ItemImage configuration
        modelBuilder.Entity<ItemImage>(entity =>
        {
            entity.HasIndex(i => i.ItemId);
            entity.HasOne(i => i.Item)
                  .WithMany(item => item.Images)
                  .HasForeignKey(i => i.ItemId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(i => i.CreatedByUser)
                  .WithMany()
                  .HasForeignKey(i => i.CreatedBy)
                  .OnDelete(DeleteBehavior.SetNull);
        });


        // AuditLog configuration
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasIndex(a => a.OrganizationId);
            entity.HasIndex(a => a.UserId);
            entity.HasIndex(a => new { a.EntityType, a.EntityId });
            entity.HasIndex(a => a.CreatedAt);
            entity.HasOne(a => a.Organization)
                  .WithMany()
                  .HasForeignKey(a => a.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(a => a.User)
                  .WithMany()
                  .HasForeignKey(a => a.UserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Notification configuration - Unified Notification Center
        modelBuilder.Entity<Notification>(entity =>
        {
            // Performance indexes for new FROM/TO pattern
            entity.HasIndex(n => new { n.ToUserId, n.ToType, n.IsRead, n.DeletedAt })
                  .HasDatabaseName("IX_Notifications_To_User_Type");

            entity.HasIndex(n => new { n.OrganizationId, n.ToType, n.CreatedAt })
                  .HasDatabaseName("IX_Notifications_Org")
                  .IsDescending(false, false, true);

            entity.HasIndex(n => new { n.Type, n.CreatedAt })
                  .HasDatabaseName("IX_Notifications_Type")
                  .IsDescending(false, true);

            // Additional useful indexes for new FROM/TO pattern
            entity.HasIndex(n => n.FromUserId);
            entity.HasIndex(n => n.ToUserId);
            entity.HasIndex(n => n.ItemId);
            entity.HasIndex(n => n.TransactionId);
            entity.HasIndex(n => n.PayoutId);

            // Configure JSONB column for PostgreSQL
            entity.Property(n => n.Payload)
                  .HasColumnType("jsonb");

            // Foreign key relationships for new FROM/TO pattern
            entity.HasOne(n => n.Organization)
                  .WithMany()
                  .HasForeignKey(n => n.OrganizationId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(n => n.FromUser)
                  .WithMany()
                  .HasForeignKey(n => n.FromUserId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(n => n.ToUser)
                  .WithMany()
                  .HasForeignKey(n => n.ToUserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(n => n.ActionCompletedByUser)
                  .WithMany()
                  .HasForeignKey(n => n.ActionCompletedByUserId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(n => n.AcknowledgedByUser)
                  .WithMany()
                  .HasForeignKey(n => n.AcknowledgedByUserId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(n => n.Item)
                  .WithMany()
                  .HasForeignKey(n => n.ItemId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(n => n.Transaction)
                  .WithMany()
                  .HasForeignKey(n => n.TransactionId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(n => n.Payout)
                  .WithMany()
                  .HasForeignKey(n => n.PayoutId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(n => n.Statement)
                  .WithMany()
                  .HasForeignKey(n => n.StatementId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(n => n.ItemRequest)
                  .WithMany()
                  .HasForeignKey(n => n.ItemRequestId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Suggestion configuration
        modelBuilder.Entity<Suggestion>(entity =>
        {
            entity.HasIndex(s => s.OrganizationId);
            entity.HasIndex(s => s.UserId);
            entity.HasIndex(s => s.Type);
            entity.HasIndex(s => s.CreatedAt);
            entity.HasIndex(s => s.IsProcessed);
            entity.HasOne(s => s.Organization)
                  .WithMany()
                  .HasForeignKey(s => s.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(s => s.User)
                  .WithMany()
                  .HasForeignKey(s => s.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // UserNotificationPreference configuration
        modelBuilder.Entity<UserNotificationPreference>(entity =>
        {
            entity.HasIndex(p => p.UserId);
            entity.HasIndex(p => p.NotificationType);
            entity.HasIndex(p => new { p.UserId, p.NotificationType }).IsUnique();
            entity.HasOne(p => p.User)
                  .WithMany()
                  .HasForeignKey(p => p.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // NotificationPreferences configuration - Unified Notification Center
        modelBuilder.Entity<NotificationPreferences>(entity =>
        {
            // Composite unique key for UserId + Role as specified in the story
            entity.HasIndex(p => new { p.UserId, p.Role })
                  .IsUnique()
                  .HasDatabaseName("UQ_NotificationPreferences_User_Role");

            // Configure JSONB column for PostgreSQL
            entity.Property(p => p.TypePreferences)
                  .HasColumnType("jsonb")
                  .IsRequired()
                  .HasDefaultValue("{}");

            // Role validation
            entity.Property(p => p.Role)
                  .IsRequired()
                  .HasMaxLength(20);

            // Foreign key relationships
            entity.HasOne(p => p.User)
                  .WithMany()
                  .HasForeignKey(p => p.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Statement configuration (Phase 2)
        modelBuilder.Entity<Statement>(entity =>
        {
            entity.HasIndex(s => s.OrganizationId);
            entity.HasIndex(s => s.ConsignorId);
            entity.HasIndex(s => new { s.ConsignorId, s.PeriodStart }).IsDescending();
            entity.HasIndex(s => new { s.OrganizationId, s.ConsignorId, s.PeriodStart }).IsUnique();

            entity.HasOne(s => s.Organization)
                  .WithMany()
                  .HasForeignKey(s => s.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(s => s.Consignor)
                  .WithMany()
                  .HasForeignKey(s => s.ConsignorId)
                  .OnDelete(DeleteBehavior.Cascade);
        });


        // SupportTicket configuration
        modelBuilder.Entity<SupportTicket>(entity =>
        {
            entity.HasIndex(st => st.Category);
            entity.HasIndex(st => st.Status);
            entity.HasIndex(st => st.AssignedTo);
            entity.HasIndex(st => st.SubmittedById);
            entity.HasIndex(st => st.CreatedAt);

            entity.HasOne(st => st.SubmittedBy)
                  .WithMany()
                  .HasForeignKey(st => st.SubmittedById)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ConsignorAgreement configuration
        modelBuilder.Entity<ConsignorAgreement>(entity =>
        {
            entity.HasKey(ca => ca.Id);
            entity.HasIndex(ca => ca.ConsignorId).IsUnique();
            entity.HasIndex(ca => ca.OrganizationId);

            entity.HasOne(ca => ca.Consignor)
                  .WithMany()
                  .HasForeignKey(ca => ca.ConsignorId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ca => ca.Organization)
                  .WithMany()
                  .HasForeignKey(ca => ca.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ca => ca.MarkedByUser)
                  .WithMany()
                  .HasForeignKey(ca => ca.MarkedByUserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Customer configuration
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasIndex(c => c.OrganizationId);
            entity.HasIndex(c => new { c.OrganizationId, c.Email }).IsUnique();
            entity.HasIndex(c => c.GoogleId).IsUnique().HasFilter("[GoogleId] IS NOT NULL");
            entity.HasIndex(c => c.AppleId).IsUnique().HasFilter("[AppleId] IS NOT NULL");
            entity.HasIndex(c => c.PhoneNumber);
            entity.HasIndex(c => c.CreatedAt);

            entity.HasOne(c => c.Organization)
                  .WithMany()
                  .HasForeignKey(c => c.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Reservation configuration
        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.HasIndex(r => r.OrganizationId);
            entity.HasIndex(r => r.CustomerId);
            entity.HasIndex(r => new { r.OrganizationId, r.Status });
            entity.HasIndex(r => r.ExpiresAt);
            entity.HasIndex(r => r.CreatedAt);

            entity.HasOne(r => r.Organization)
                  .WithMany()
                  .HasForeignKey(r => r.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.Customer)
                  .WithMany(c => c.Reservations)
                  .HasForeignKey(r => r.CustomerId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(r => r.CreatedByUser)
                  .WithMany()
                  .HasForeignKey(r => r.CreatedBy)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(r => r.UpdatedByUser)
                  .WithMany()
                  .HasForeignKey(r => r.UpdatedBy)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // ReservationItem configuration
        modelBuilder.Entity<ReservationItem>(entity =>
        {
            entity.HasIndex(ri => ri.ReservationId);
            entity.HasIndex(ri => ri.ItemId);
            entity.HasIndex(ri => new { ri.ReservationId, ri.ItemId }).IsUnique();

            entity.HasOne(ri => ri.Reservation)
                  .WithMany(r => r.ReservationItems)
                  .HasForeignKey(ri => ri.ReservationId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ri => ri.Item)
                  .WithMany()
                  .HasForeignKey(ri => ri.ItemId)
                  .OnDelete(DeleteBehavior.Restrict); // Don't allow deleting items that are reserved
        });

        // Seed Data
        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        // Predefined IDs for consistent seeding
        var orgId = new Guid("11111111-1111-1111-1111-111111111111");
        var adminUserId = new Guid("22222222-2222-2222-2222-222222222222");
        var shopOwnerUserId = new Guid("33333333-3333-3333-3333-333333333333");
        var consignorUserId = new Guid("44444444-4444-4444-4444-444444444444");
        var customerUserId = new Guid("55555555-5555-5555-5555-555555555555");
        var consignorId = new Guid("66666666-6666-6666-6666-666666666666");

        // Seed Organization
        modelBuilder.Entity<Organization>().HasData(
            new Organization
            {
                Id = orgId,
                Name = "Demo Consignment Shop",
                VerticalType = VerticalType.Consignment,
                Subdomain = "demo-shop",
                CachedSubscriptionStatus = "active",
                IsFounderPricing = true,
                FounderTier = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        );

        // Seed Users - all with password "password123"
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword("password123");

        modelBuilder.Entity<User>().HasData(
            // Admin (Owner role)
            new User
            {
                Id = adminUserId,
                Email = "admin@microsaasbuilders.com",
                PasswordHash = hashedPassword,
                Role = UserRole.Owner,
                OrganizationId = orgId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            // Shop Owner (Owner role)
            new User
            {
                Id = shopOwnerUserId,
                Email = "owner1@microsaasbuilders.com",
                PasswordHash = hashedPassword,
                Role = UserRole.Owner,
                OrganizationId = orgId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            // Consignor (Consignor role)
            new User
            {
                Id = consignorUserId,
                Email = "consignor1@microsaasbuilders.com",
                PasswordHash = hashedPassword,
                Role = UserRole.Consignor,
                OrganizationId = orgId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        );

        // Seed Consignor entity for the consignor user
        modelBuilder.Entity<Consignor>().HasData(
            new Consignor
            {
                Id = consignorId,
                OrganizationId = orgId,
                UserId = consignorUserId,
                ConsignorNumber = "PRV-00001",
                Name = "Demo Artist",
                PreferredName = "Demo",
                Email = "consignor1@microsaasbuilders.com",
                Phone = "(555) 123-4567",
                CommissionRate = 0.6000m, // 60%
                Status = ConsignorStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        );

        // IntegrationCredentials configuration
        modelBuilder.Entity<IntegrationCredentials>(entity =>
        {
            entity.HasIndex(ic => ic.OrganizationId);
            entity.HasIndex(ic => new { ic.OrganizationId, ic.IntegrationType }).IsUnique();
            entity.HasOne(ic => ic.Organization)
                  .WithMany()
                  .HasForeignKey(ic => ic.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Organization indexes for new fields
        modelBuilder.Entity<Organization>(entity =>
        {
            entity.HasIndex(o => o.Slug).IsUnique();
            entity.HasIndex(o => o.StoreCode).IsUnique();
            entity.HasIndex(o => o.SetupStep);
            entity.HasIndex(o => o.CachedSubscriptionStatus);
            entity.HasIndex(o => o.StripeCustomerId);
            entity.HasIndex(o => o.StripeSubscriptionId);
        });

        // ConsignorInvitation configuration
        modelBuilder.Entity<ConsignorInvitation>(entity =>
        {
            entity.ToTable("ConsignorInvitations");
            entity.HasIndex(pi => pi.OrganizationId);
            entity.HasIndex(pi => pi.Token).IsUnique();
            entity.HasIndex(pi => new { pi.OrganizationId, pi.Email });
            entity.HasIndex(pi => pi.Status);
            entity.HasIndex(pi => pi.ExpiresAt);
            entity.HasOne(pi => pi.Organization)
                  .WithMany()
                  .HasForeignKey(pi => pi.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(pi => pi.InvitedBy)
                  .WithMany()
                  .HasForeignKey(pi => pi.InvitedById)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // ClerkInvitation configuration
        modelBuilder.Entity<ClerkInvitation>(entity =>
        {
            entity.HasIndex(ci => ci.OrganizationId);
            entity.HasIndex(ci => ci.Token).IsUnique();
            entity.HasIndex(ci => new { ci.OrganizationId, ci.Email });
            entity.HasIndex(ci => ci.Status);
            entity.HasIndex(ci => ci.ExpiresAt);
            entity.HasOne(ci => ci.Organization)
                  .WithMany()
                  .HasForeignKey(ci => ci.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(ci => ci.InvitedBy)
                  .WithMany()
                  .HasForeignKey(ci => ci.InvitedById)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // OwnerInvitation configuration
        modelBuilder.Entity<OwnerInvitation>(entity =>
        {
            entity.HasIndex(oi => oi.Token).IsUnique();
            entity.HasIndex(oi => oi.Email);
            entity.HasIndex(oi => oi.Status);
            entity.HasIndex(oi => oi.ExpiresAt);
            entity.HasOne(oi => oi.InvitedBy)
                  .WithMany()
                  .HasForeignKey(oi => oi.InvitedById)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // ItemRequest configuration
        modelBuilder.Entity<ItemRequest>(entity =>
        {
            entity.HasIndex(ir => ir.OrganizationId);
            entity.HasIndex(ir => ir.ConsignorId);
            entity.HasIndex(ir => ir.Status);
            entity.HasIndex(ir => ir.CreatedAt);
            entity.HasOne(ir => ir.Organization)
                  .WithMany()
                  .HasForeignKey(ir => ir.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(ir => ir.Consignor)
                  .WithMany()
                  .HasForeignKey(ir => ir.ConsignorId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(ir => ir.ReviewedByUser)
                  .WithMany()
                  .HasForeignKey(ir => ir.ReviewedBy)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(ir => ir.ApprovedItem)
                  .WithMany()
                  .HasForeignKey(ir => ir.ApprovedItemId)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(ir => ir.OriginalRequest)
                  .WithMany()
                  .HasForeignKey(ir => ir.OriginalRequestId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // ItemRequestImage configuration
        modelBuilder.Entity<ItemRequestImage>(entity =>
        {
            entity.HasIndex(iri => iri.ItemRequestId);
            entity.HasOne(iri => iri.ItemRequest)
                  .WithMany(ir => ir.Images)
                  .HasForeignKey(iri => iri.ItemRequestId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ConsignorAgreementUpload configuration
        modelBuilder.Entity<ConsignorAgreementUpload>(entity =>
        {
            entity.HasIndex(cau => cau.ConsignorId);
            entity.HasIndex(cau => cau.UploadedAt);
            entity.HasIndex(cau => cau.ReviewedAt);
            entity.HasIndex(cau => cau.ReviewDecision);

            entity.Property(cau => cau.VerificationResult)
                  .HasColumnType("jsonb");

            entity.HasOne(cau => cau.Consignor)
                  .WithMany()
                  .HasForeignKey(cau => cau.ConsignorId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(cau => cau.ReviewedByUser)
                  .WithMany()
                  .HasForeignKey(cau => cau.ReviewedBy)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // DropoffRequest configuration
        modelBuilder.Entity<DropoffRequest>(entity =>
        {
            entity.HasIndex(dr => dr.OrganizationId);
            entity.HasIndex(dr => dr.ConsignorId);
            entity.HasIndex(dr => dr.Status);
            entity.HasIndex(dr => dr.PlannedDate);
            entity.HasIndex(dr => dr.CreatedAt);

            entity.Property(dr => dr.ItemsJson)
                  .HasColumnType("jsonb");

            entity.Property(dr => dr.Status)
                  .HasMaxLength(20);

            entity.Property(dr => dr.PlannedTimeSlot)
                  .HasMaxLength(50);

            entity.Property(dr => dr.CancelReason)
                  .HasMaxLength(200);

            entity.Property(dr => dr.SuggestedTotal)
                  .HasColumnType("decimal(10,2)");

            entity.HasOne(dr => dr.Organization)
                  .WithMany()
                  .HasForeignKey(dr => dr.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(dr => dr.Consignor)
                  .WithMany()
                  .HasForeignKey(dr => dr.ConsignorId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // PendingImportItem configuration
        modelBuilder.Entity<PendingImportItem>(entity =>
        {
            entity.HasIndex(pii => pii.OrganizationId);
            entity.HasIndex(pii => pii.ConsignorId);
            entity.HasIndex(pii => pii.Source);
            entity.HasIndex(pii => pii.Status);
            entity.HasIndex(pii => pii.SourceReference);
            entity.HasIndex(pii => pii.CreatedAt);
            entity.HasIndex(pii => new { pii.OrganizationId, pii.Status, pii.Source });

            entity.Property(pii => pii.Name)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(pii => pii.Price)
                  .HasColumnType("decimal(10,2)");

            entity.Property(pii => pii.Sku)
                  .HasMaxLength(100);

            entity.Property(pii => pii.Category)
                  .HasMaxLength(100);

            entity.Property(pii => pii.Condition)
                  .HasMaxLength(50);

            entity.Property(pii => pii.SourceReference)
                  .HasMaxLength(200);

            entity.Property(pii => pii.Source)
                  .HasConversion<int>();

            entity.Property(pii => pii.Status)
                  .HasConversion<int>();

            entity.HasOne(pii => pii.Organization)
                  .WithMany()
                  .HasForeignKey(pii => pii.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(pii => pii.Consignor)
                  .WithMany()
                  .HasForeignKey(pii => pii.ConsignorId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(pii => pii.ImportedItem)
                  .WithMany()
                  .HasForeignKey(pii => pii.ImportedItemId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // PayoutSettings configuration
        modelBuilder.Entity<PayoutSettings>(entity =>
        {
            entity.HasIndex(ps => ps.OrganizationId).IsUnique();
            entity.Property(ps => ps.ExtendedSettings)
                  .HasColumnType("jsonb")
                  .HasDefaultValue("{}");
            entity.HasOne(ps => ps.Organization)
                  .WithMany()
                  .HasForeignKey(ps => ps.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // PayoutSummary configuration
        modelBuilder.Entity<PayoutSummary>(entity =>
        {
            entity.HasIndex(ps => new { ps.OrganizationId, ps.ConsignorId }).IsUnique();
            entity.HasIndex(ps => ps.OrganizationId);
            entity.HasIndex(ps => new { ps.OrganizationId, ps.MeetsMinimumThreshold });
            entity.HasIndex(ps => ps.LastComputedAt);

            entity.HasOne(ps => ps.Organization)
                  .WithMany()
                  .HasForeignKey(ps => ps.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ps => ps.Consignor)
                  .WithMany()
                  .HasForeignKey(ps => ps.ConsignorId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // BookkeepingSettings configuration
        modelBuilder.Entity<BookkeepingSettings>(entity =>
        {
            entity.HasIndex(bs => bs.OrganizationId).IsUnique();
            entity.Property(bs => bs.AccountMappings)
                  .HasColumnType("jsonb")
                  .HasDefaultValue("{}");
            entity.HasOne(bs => bs.Organization)
                  .WithMany()
                  .HasForeignKey(bs => bs.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // QBSyncLog configuration
        modelBuilder.Entity<QBSyncLog>(entity =>
        {
            entity.HasIndex(qsl => new { qsl.ShopId, qsl.EntityId, qsl.Action });
            entity.HasOne(qsl => qsl.Organization)
                  .WithMany()
                  .HasForeignKey(qsl => qsl.ShopId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entities = ChangeTracker.Entries()
            .Where(x => x.Entity is BaseEntity && (x.State == EntityState.Modified || x.State == EntityState.Added))
            .Select(x => x.Entity as BaseEntity);

        foreach (var entity in entities)
        {
            if (entity != null)
            {
                entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        // Handle SupportTicket auto-routing logic
        var supportTickets = ChangeTracker.Entries<SupportTicket>()
            .Where(x => x.State == EntityState.Added)
            .Select(x => x.Entity);

        foreach (var supportTicket in supportTickets)
        {
            // Auto-routing logic: content → owner, others → admin
            // Only apply auto-routing if AssignedTo is not explicitly set
            if (supportTicket.AssignedTo == SupportTicketAssignedTo.NotAssigned)
            {
                supportTicket.AssignedTo = supportTicket.Category == SupportTicketCategory.Content
                    ? SupportTicketAssignedTo.Owner
                    : SupportTicketAssignedTo.Admin;
            }
        }
    }
}