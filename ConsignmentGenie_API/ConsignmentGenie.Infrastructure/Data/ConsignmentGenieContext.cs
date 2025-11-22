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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Organization configuration
        modelBuilder.Entity<Organization>(entity =>
        {
            entity.HasIndex(o => o.Name);
            entity.HasIndex(o => o.Subdomain).IsUnique();
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
            entity.HasIndex(p => new { p.OrganizationId, p.Email }).IsUnique();
            entity.HasOne(p => p.Organization)
                  .WithMany(o => o.Providers)
                  .HasForeignKey(p => p.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(p => p.User)
                  .WithOne(u => u.Provider)
                  .HasForeignKey<Provider>(p => p.UserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Item configuration
        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasIndex(i => new { i.OrganizationId, i.SKU }).IsUnique();
            entity.HasOne(i => i.Organization)
                  .WithMany(o => o.Items)
                  .HasForeignKey(i => i.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(i => i.Provider)
                  .WithMany(p => p.Items)
                  .HasForeignKey(i => i.ProviderId)
                  .OnDelete(DeleteBehavior.Restrict);
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

        // ItemTagAssignment configuration
        modelBuilder.Entity<ItemTagAssignment>(entity =>
        {
            entity.HasKey(e => new { e.ItemId, e.ItemTagId });
            entity.HasOne(e => e.Item)
                  .WithMany(i => i.ItemTagAssignments)
                  .HasForeignKey(e => e.ItemId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.ItemTag)
                  .WithMany(t => t.ItemTagAssignments)
                  .HasForeignKey(e => e.ItemTagId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Update Item configuration to include new relationships
        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasOne(i => i.ItemCategory)
                  .WithMany(c => c.Items)
                  .HasForeignKey(i => i.CategoryId)
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

        // Notification configuration
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasIndex(n => n.OrganizationId);
            entity.HasIndex(n => n.UserId);
            entity.HasIndex(n => new { n.UserId, n.IsRead });
            entity.HasIndex(n => n.CreatedAt);
            entity.HasOne(n => n.Organization)
                  .WithMany()
                  .HasForeignKey(n => n.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(n => n.User)
                  .WithMany()
                  .HasForeignKey(n => n.UserId)
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
                DisplayName = "Demo Artist",
                Email = "provider@demoshop.com",
                Phone = "(555) 123-4567",
                DefaultSplitPercentage = 60.00m,
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