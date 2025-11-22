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
    public DbSet<Provider> Providers { get; set; }
    public DbSet<Item> Items { get; set; }
    public DbSet<ItemImage> ItemImages { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Payout> Payouts { get; set; }
    public DbSet<SubscriptionEvent> SubscriptionEvents { get; set; }
    public DbSet<SquareConnection> SquareConnections { get; set; }
    public DbSet<SquareSyncLog> SquareSyncLogs { get; set; }
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
    public DbSet<Shopper> Shoppers { get; set; }
    public DbSet<GuestCheckout> GuestCheckouts { get; set; }

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

        // Provider configuration
        modelBuilder.Entity<Provider>(entity =>
        {
            entity.HasIndex(p => new { p.OrganizationId, p.ProviderNumber }).IsUnique();
            entity.HasIndex(p => new { p.OrganizationId, p.Email }).IsUnique();
            entity.HasIndex(p => new { p.OrganizationId, p.Status });
            entity.HasIndex(p => p.ApprovalStatus);

            entity.HasOne(p => p.Organization)
                  .WithMany(o => o.Providers)
                  .HasForeignKey(p => p.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(p => p.User)
                  .WithOne(u => u.Provider)
                  .HasForeignKey<Provider>(p => p.UserId)
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
            entity.HasIndex(i => new { i.OrganizationId, i.Category });
            entity.HasOne(i => i.Organization)
                  .WithMany(o => o.Items)
                  .HasForeignKey(i => i.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(i => i.Provider)
                  .WithMany(p => p.Items)
                  .HasForeignKey(i => i.ProviderId)
                  .OnDelete(DeleteBehavior.Restrict);
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
            entity.HasOne(t => t.Item)
                  .WithOne(i => i.Transaction)
                  .HasForeignKey<Transaction>(t => t.ItemId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(t => t.Provider)
                  .WithMany(p => p.Transactions)
                  .HasForeignKey(t => t.ProviderId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Payout configuration
        modelBuilder.Entity<Payout>(entity =>
        {
            entity.HasIndex(p => p.OrganizationId);
            entity.HasIndex(p => new { p.ProviderId, p.PeriodStart, p.PeriodEnd });
            entity.HasOne(p => p.Organization)
                  .WithMany(o => o.Payouts)
                  .HasForeignKey(p => p.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(p => p.Provider)
                  .WithMany(pr => pr.Payouts)
                  .HasForeignKey(p => p.ProviderId)
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
            entity.HasOne(s => s.Organization)
                  .WithMany()
                  .HasForeignKey(s => s.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(s => s.SyncLogs)
                  .WithOne()
                  .HasForeignKey(sl => sl.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // SquareSyncLog configuration
        modelBuilder.Entity<SquareSyncLog>(entity =>
        {
            entity.HasIndex(s => s.OrganizationId);
            entity.HasIndex(s => s.SyncStarted);
            entity.HasOne(s => s.Organization)
                  .WithMany()
                  .HasForeignKey(s => s.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Transaction - Add Square index
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasIndex(t => t.SquarePaymentId).IsUnique();
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

        // Category configuration
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasIndex(c => new { c.OrganizationId, c.Name }).IsUnique();
            entity.HasOne(c => c.Organization)
                  .WithMany()
                  .HasForeignKey(c => c.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(c => c.CreatedByUser)
                  .WithMany()
                  .HasForeignKey(c => c.CreatedBy)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(c => c.UpdatedByUser)
                  .WithMany()
                  .HasForeignKey(c => c.UpdatedBy)
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

        // Notification configuration (Updated for Phase 2)
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasIndex(n => n.OrganizationId);
            entity.HasIndex(n => n.UserId);
            entity.HasIndex(n => n.ProviderId);
            entity.HasIndex(n => new { n.UserId, n.IsRead });
            entity.HasIndex(n => n.CreatedAt);
            entity.HasIndex(n => n.Type);

            entity.HasOne(n => n.Organization)
                  .WithMany()
                  .HasForeignKey(n => n.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(n => n.User)
                  .WithMany()
                  .HasForeignKey(n => n.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(n => n.Provider)
                  .WithMany()
                  .HasForeignKey(n => n.ProviderId)
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

        // NotificationPreferences configuration (Phase 2)
        modelBuilder.Entity<NotificationPreferences>(entity =>
        {
            entity.HasIndex(p => p.UserId).IsUnique();
            entity.HasOne(p => p.User)
                  .WithOne()
                  .HasForeignKey<NotificationPreferences>(p => p.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Statement configuration (Phase 2)
        modelBuilder.Entity<Statement>(entity =>
        {
            entity.HasIndex(s => s.OrganizationId);
            entity.HasIndex(s => s.ProviderId);
            entity.HasIndex(s => new { s.ProviderId, s.PeriodStart }).IsDescending();
            entity.HasIndex(s => new { s.OrganizationId, s.ProviderId, s.PeriodStart }).IsUnique();

            entity.HasOne(s => s.Organization)
                  .WithMany()
                  .HasForeignKey(s => s.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(s => s.Provider)
                  .WithMany()
                  .HasForeignKey(s => s.ProviderId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Shopper configuration
        modelBuilder.Entity<Shopper>(entity =>
        {
            entity.HasIndex(s => s.OrganizationId);
            entity.HasIndex(s => s.UserId).IsUnique();
            entity.HasIndex(s => new { s.OrganizationId, s.Email }).IsUnique();
            entity.HasOne(s => s.Organization)
                  .WithMany()
                  .HasForeignKey(s => s.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(s => s.User)
                  .WithOne()
                  .HasForeignKey<Shopper>(s => s.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // GuestCheckout configuration
        modelBuilder.Entity<GuestCheckout>(entity =>
        {
            entity.HasIndex(g => g.OrganizationId);
            entity.HasIndex(g => g.SessionToken).IsUnique();
            entity.HasIndex(g => g.ExpiresAt);
            entity.HasOne(g => g.Organization)
                  .WithMany()
                  .HasForeignKey(g => g.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);
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
        var providerUserId = new Guid("44444444-4444-4444-4444-444444444444");
        var customerUserId = new Guid("55555555-5555-5555-5555-555555555555");
        var providerId = new Guid("66666666-6666-6666-6666-666666666666");

        // Seed Organization
        modelBuilder.Entity<Organization>().HasData(
            new Organization
            {
                Id = orgId,
                Name = "Demo Consignment Shop",
                VerticalType = VerticalType.Consignment,
                SubscriptionStatus = SubscriptionStatus.Active,
                SubscriptionTier = SubscriptionTier.Pro,
                Subdomain = "demo-shop",
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
                Email = "admin@demoshop.com",
                PasswordHash = hashedPassword,
                Role = UserRole.Owner,
                OrganizationId = orgId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            // Shop Owner (Manager role)
            new User
            {
                Id = shopOwnerUserId,
                Email = "owner@demoshop.com",
                PasswordHash = hashedPassword,
                Role = UserRole.Manager,
                OrganizationId = orgId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            // Provider (Provider role)
            new User
            {
                Id = providerUserId,
                Email = "provider@demoshop.com",
                PasswordHash = hashedPassword,
                Role = UserRole.Provider,
                OrganizationId = orgId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            // Customer (Customer role)
            new User
            {
                Id = customerUserId,
                Email = "customer@demoshop.com",
                PasswordHash = hashedPassword,
                Role = UserRole.Customer,
                OrganizationId = orgId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        );

        // Seed Provider entity for the provider user
        modelBuilder.Entity<Provider>().HasData(
            new Provider
            {
                Id = providerId,
                OrganizationId = orgId,
                UserId = providerUserId,
                ProviderNumber = "PRV-00001",
                FirstName = "Demo",
                LastName = "Artist",
                Email = "provider@demoshop.com",
                Phone = "(555) 123-4567",
                CommissionRate = 0.6000m, // 60%
                Status = ProviderStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        );
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
            .Where(x => x.Entity is BaseEntity && x.State == EntityState.Modified)
            .Select(x => x.Entity as BaseEntity);

        foreach (var entity in entities)
        {
            if (entity != null)
            {
                entity.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}