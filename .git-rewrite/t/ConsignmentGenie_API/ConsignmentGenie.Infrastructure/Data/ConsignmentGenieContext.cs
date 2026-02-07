using ConsignmentGenie.Core.Entities;
using Microsoft.EntityFrameworkCore;

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